using System;
using System.Linq;
using System.Threading;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Rectangle = System.Drawing.Rectangle;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using SharpDX.Mathematics.Interop;
using Factory2 = SharpDX.DXGI.Factory2;
using Resource = SharpDX.DXGI.Resource;
using Device = SharpDX.Direct3D11.Device;
using Texture = SharpDX.Direct3D11.Texture2D;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using D2DAlphaMode = SharpDX.Direct2D1.AlphaMode;
using D2DPixelFormat = SharpDX.Direct2D1.PixelFormat;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using TextureDescription = SharpDX.Direct3D11.Texture2DDescription;

using AyeBlinkin.Serial;
using AyeBlinkin.Centroid;
using Message = AyeBlinkin.Serial.Message;

namespace AyeBlinkin.DirectX 
{
    internal class HardwareScreenCapture<T> : IDisposable where T : ICentroidColor, new()
    {
        private const int MAX_RECTANGLE_SIZE = 25;
        private Device device;
        private Output1 output;
        private Adapter1 adapter;
        private Resource capture;
        private OutputDuplication duplicator;
        private Texture gpuTexture;
        private Texture cpuTexture;
        private ShaderResourceView scaler;
        private IntPtr window = IntPtr.Zero;
        private Texture renderTexture;
        private Rectangle renderBounds;
        private SwapChain1 renderWindow;
        private RenderTarget renderOverlay;
        private RenderTargetView renderTarget;
        private SwapChainDescription1 renderDescription;
        private TextFormat fpsFont;
        private RectangleF fpsLocation;
        private AvgFPSCounter fpsCounter;
        private SolidColorBrush fpsColor;
        private byte[] memBuffer;
        private GCHandle pinnedMemBuffer;
        private IntPtr ptrMemBuffer;
        private int stride; // width * 4 (argb)
        private Rectangle[] ledPoints;
        private List<int> range;
        private static readonly int WaitTimeout = SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code;
        private static readonly PresentParameters presentParameters = new PresentParameters();
        private ICentroidColor colorFinder;

        public void Dispose() 
        {
            if(pinnedMemBuffer.IsAllocated)
                pinnedMemBuffer.Free();

            TryReleaseFrame();
            if(Settings.SettingsHwnd != IntPtr.Zero) { // restore window background color
                device?.ImmediateContext.ClearRenderTargetView(renderTarget, SharpDX.Color.WhiteSmoke);
                renderWindow?.Present(0, PresentFlags.None);
            }

            Settings.Model.PropertyChanged -= LedsChanged;
            fpsCounter?.Dispose();
            fpsColor?.Dispose();
            fpsFont?.Dispose();
            renderOverlay?.Dispose();
            renderTarget?.Dispose();
            renderTexture?.Dispose();
            renderWindow?.Dispose();
            device?.ImmediateContext?.ClearState();
            device?.ImmediateContext?.Flush();
            device?.ImmediateContext?.Dispose();
            adapter?.Dispose();
            output?.Dispose();
            device?.Dispose();
            duplicator?.Dispose();
            capture?.Dispose();
            scaler?.Dispose();
            gpuTexture?.Dispose();
            cpuTexture?.Dispose();
        }

        private HardwareScreenCapture() => colorFinder = new T();

        internal void Initialize() 
        {
            adapter = DeviceEnumerator.GetAdapter(int.Parse(Settings.Model.AdapterId));
            output = adapter.GetOutput1(Settings.Model.DisplayId);

            var scale = Settings.Scale;
            var bounds = output.Description.DesktopBounds;
            var outputWidth = bounds.Right - bounds.Left;
            var outputHeight = bounds.Bottom - bounds.Top;

            renderBounds = new Rectangle(0, 0, outputWidth / scale, outputHeight / scale);
            fpsLocation = new RectangleF(renderBounds.Width-92, renderBounds.Height-19, renderBounds.Width, renderBounds.Height);

            device = new Device(adapter, DeviceCreationFlags.PreventAlteringLayerSettingsFromRegistry 
                | DeviceCreationFlags.SingleThreaded
                | DeviceCreationFlags.BgraSupport);

            gpuTexture = new Texture2D(device, new TextureDescription() 
            {
                CpuAccessFlags = CpuAccessFlags.None,
                BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
                Format = Format.B8G8R8A8_UNorm,
                Width = outputWidth,
                Height = outputHeight,
                OptionFlags = ResourceOptionFlags.GenerateMipMaps,
                MipLevels = (int)Math.Log2(scale) + 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Default
            });

            cpuTexture = new Texture2D(device, new TextureDescription() 
            {
                CpuAccessFlags = CpuAccessFlags.Read,
                BindFlags = BindFlags.None,
                Format = Format.B8G8R8A8_UNorm,
                Width = renderBounds.Width,
                Height = renderBounds.Height,
                OptionFlags = ResourceOptionFlags.None,
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = { Count = 1, Quality = 0 },
                Usage = ResourceUsage.Staging
            });

            renderDescription = new SwapChainDescription1() 
            {
                BufferCount = 1,
                Width = renderBounds.Width,
                Stereo = false,
                Height = renderBounds.Height,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                Scaling = Scaling.Stretch,
                SwapEffect = SwapEffect.Discard
            };

            stride = renderBounds.Width * 4;
            memBuffer = new byte[renderBounds.Height * stride];
            pinnedMemBuffer = GCHandle.Alloc(memBuffer, GCHandleType.Pinned);
            ptrMemBuffer = pinnedMemBuffer.AddrOfPinnedObject();
            
            generateRecPoints();
            Settings.Model.PropertyChanged += LedsChanged;

            duplicator = output.DuplicateOutput(device);
            scaler = new ShaderResourceView(device, gpuTexture);
        }

        private bool CaptureFrameGPU() 
        {
            //uncomment for double vsync
            //output.WaitForVerticalBlank();
            output.WaitForVerticalBlank();

            var result = duplicator.TryAcquireNextFrame(500, out _, out capture);
            using(capture)
            {
                if(result.Code == WaitTimeout)
                    return false;

                if(result.Failure)
                    result.CheckError(); //throw
            
                using(var texture = capture.QueryInterface<Texture2D>())
                    device.ImmediateContext.CopySubresourceRegion(texture, 0, null, gpuTexture, 0);
            }
            
            TryReleaseFrame();
            device.ImmediateContext.GenerateMips(scaler);
            device.ImmediateContext.CopySubresourceRegion(gpuTexture, gpuTexture.Description.MipLevels - 1, null, cpuTexture, 0);
            return true;
        }

        private void Render(byte[] points)
        {
            if(CheckRender(Settings.SettingsHwnd)) 
            {
                device.ImmediateContext.CopySubresourceRegion(gpuTexture, gpuTexture.Description.MipLevels - 1, null, renderTexture, 0);
                
                renderOverlay.BeginDraw();
                renderOverlay.DrawText($"FPS: {fpsCounter.NextFPS():00.00}", fpsFont, fpsLocation, fpsColor);
                
                for(var i = 0; i < ledPoints.Length; i++) {
                    var r = points[(i*3)] / 255F;
                    var g = points[(i*3)+1] / 255F;
                    var b = points[(i*3)+2] / 255F;
                    var rec = ledPoints[i];
                    var x = rec.Width / 2F;
                    var y = rec.Height / 2F;

                    var selected = i == Settings.Model.PreviewLED? 1F : 0F;
                    using(var color = new SolidColorBrush(renderOverlay, new Color4(r, g, b, 1F)))
                    using(var outline = new SolidColorBrush(renderOverlay, new Color4(selected, selected, selected, 1F))) {
                        renderOverlay.FillEllipse(new Ellipse(new RawVector2(rec.X + x, rec.Y + y), x/2, y/2), color);
                        renderOverlay.DrawEllipse(new Ellipse(new RawVector2(rec.X + x, rec.Y + y), x/2, y/2), outline);
                    }
                }

                //TODO: draw volume bar meter
                renderOverlay.EndDraw();
                
                renderWindow.Present(0, PresentFlags.None, presentParameters);
            }
        }

        private bool CheckRender(IntPtr hwnd) {
            //window did not change
            if(hwnd == window) 
                return window != IntPtr.Zero; //is not no window

            //window changed do some cleanup of resource attached to old window
            fpsColor?.Dispose();
            fpsFont?.Dispose();
            renderOverlay?.Dispose();
            renderTarget?.Dispose();
            renderTexture?.Dispose();
            renderWindow?.Dispose();
            fpsCounter?.Dispose();
            window = hwnd;

            //window changed to no window
            if(window == IntPtr.Zero)
                return false;

            //window render init
            using(var factory = adapter.GetParent<Factory2>()) {
                renderWindow = new SwapChain1(factory, device, window, ref renderDescription, 
                    new SwapChainFullScreenDescription() {
                        RefreshRate = new Rational(15, 1),
                        Scaling = DisplayModeScaling.Stretched,
                        Windowed = true
                    }, null);

                factory.MakeWindowAssociation(window, WindowAssociationFlags.IgnoreAll);
            }

            renderTexture = Texture2D.FromSwapChain<Texture2D>(renderWindow, 0);
            renderTarget = new RenderTargetView(device, renderTexture);

            //overlay init
            using(var surface = renderWindow.GetBackBuffer<Surface>(0))
            using(var factory = new SharpDX.Direct2D1.Factory(SharpDX.Direct2D1.FactoryType.SingleThreaded))
                renderOverlay =  new RenderTarget(factory, surface, new RenderTargetProperties() {
                    Type = RenderTargetType.Default,
                    PixelFormat = new D2DPixelFormat(Format.B8G8R8A8_UNorm, D2DAlphaMode.Premultiplied)}) { 
                    TextAntialiasMode = TextAntialiasMode.Cleartype 
                };

            using(var factory = new SharpDX.DirectWrite.Factory(SharpDX.DirectWrite.FactoryType.Shared))
                fpsFont = new TextFormat(factory, "Calibri", FontWeight.Bold, FontStyle.Normal, 18);

            fpsColor = new SolidColorBrush(renderOverlay, new Color4(1f, 0f, 1f, 0.7f));
            fpsCounter = new AvgFPSCounter();
            return true;
        }

        private bool TryReleaseFrame() 
        {
            try 
            {
                duplicator?.ReleaseFrame();
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void LedsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch(e.PropertyName) 
            {
                case nameof(Settings.Model.HorizontalLEDs):
                case nameof(Settings.Model.VerticalLEDs):
                case nameof(Settings.Model.Mirror):
                    generateRecPoints();
                    SerialCom.Enqueue(Message.StreamStart());
                    break;
            }
        }

        private void generateRecPoints() 
        {
            const int padding = 2;
            var points = new List<Rectangle>();

            var w = renderBounds.Width - (2 * padding);
            var hcount = Settings.Model.HorizontalLEDs;
            var width = w / hcount;
            var wmod = w - (width * hcount);

            var x = padding;
            var topleftright = Enumerable.Range(0, hcount).Select(z => {
                var paddedWidth = width;
                
                if(wmod-- > 0)
                    paddedWidth++;

                var r = new Rectangle(x, padding, paddedWidth, MAX_RECTANGLE_SIZE);
                x += paddedWidth;
                return r;
            }).ToList();

            var y = renderBounds.Height - padding - MAX_RECTANGLE_SIZE;
            var bottomleftright = topleftright
                .Select(r => new Rectangle(r.X, y, r.Width, r.Height))
                .ToList();


            var h = renderBounds.Height - padding - MAX_RECTANGLE_SIZE;
            var vcount = Settings.Model.VerticalLEDs;
            var height = h / vcount;
            var hmod = h - (height * vcount);

            y = MAX_RECTANGLE_SIZE;
            var lefttopbottom = Enumerable.Range(0, vcount).Select(z => {
                var paddedHeight = height;

                if(hmod-- > 0) 
                    paddedHeight++;

                var r = new Rectangle(padding, y, MAX_RECTANGLE_SIZE, paddedHeight);
                y += paddedHeight;
                return r;
            }).ToList();

            x = renderBounds.Width - padding - MAX_RECTANGLE_SIZE;
            var righttopbottom = lefttopbottom
                .Select(r => new Rectangle(x, r.Y, r.Width, r.Height))
                .ToList();

            points.AddRange(lefttopbottom.AsEnumerable().Reverse());
            points.AddRange(topleftright);
            points.AddRange(righttopbottom);

            //points.AddRange(bottomleftright.AsEnumerable().Reverse());
            ledPoints = points.ToArray();
            range = Enumerable.Range(0, ledPoints.Length).ToList();
        }

        private void TransferFrameCPU() 
        {
            var source = device.ImmediateContext.MapSubresource(cpuTexture, 0, MapMode.Read, MapFlags.None);
            var sourcePtr = source.DataPointer;
            var destPtr = ptrMemBuffer;
            var height = renderBounds.Height;

            for (int i = 0; i < height; i++)
            {
                Utilities.CopyMemory(destPtr, sourcePtr, stride);
                sourcePtr = IntPtr.Add(sourcePtr, source.RowPitch);
                destPtr = IntPtr.Add(destPtr, stride);
            }

            device.ImmediateContext.UnmapSubresource(cpuTexture, 0);
        }

        private List<byte[]> CalculatePoints()
        {
#if POINTS_TIMER
            var sw = new Stopwatch();
            sw.Start();
#endif
            var points = range.AsParallel().AsOrdered()
                .Select(ix => colorFinder.Calculate(ref memBuffer, ref ledPoints, ix, renderBounds.Width))
                .ToList();

#if POINTS_TIMER
            Console.WriteLine(sw.Elapsed.TotalMilliseconds);
#endif
            return points;
        }

        private class AvgFPSCounter : IDisposable
        {
            private const int framesToAverage = 30;
            private const float msScale = 1000F * framesToAverage;
            private float msTotal = 0F;
            private int dataPointer = 0;
            private float[] data = new float[framesToAverage];
            private Stopwatch timer = new Stopwatch();
            public AvgFPSCounter() => timer.Start();

            public void Dispose() {
                timer?.Stop();
                timer = null;
            }

            public double NextFPS() 
            {
                var ms = (float)timer.Elapsed.TotalMilliseconds;
                timer.Restart();

                msTotal += ms - data[dataPointer];
                data[dataPointer] = ms;

                dataPointer = ++dataPointer % framesToAverage;
                return msScale / msTotal;
            }
        }

#region Thread Loop
        internal static void Run(object obj)
        {
            Thread.CurrentThread.Name = "DXGI Capture";
            var token = (CancellationToken)obj;
            List<byte[]> results;
            byte[] points;
            int i, offset;

            HardwareScreenCapture<T> instance = null;
            while(!token.IsCancellationRequested) 
            {
                try 
                {
                    if(instance == null) 
                    {
                        instance = new HardwareScreenCapture<T>();
                        instance.Initialize();
                        SerialCom.Enqueue(Message.StreamStart());
                    }

                    if(instance.CaptureFrameGPU()) 
                    {
                        instance.TransferFrameCPU();
                        results = instance.CalculatePoints();
                        points = new byte[results.Count * 3];

                        for(i = 0, offset = 0; i < results.Count; i++) 
                        {
                            System.Buffer.BlockCopy(results[i], 0, points, offset, 3);
                            offset += 3;
                        }

                        if(token.IsCancellationRequested)
                            break;

                        SerialCom.Enqueue(new Message.Command() {
                            Type = Message.Type.Stream,
                            Raw = points
                        });

                        instance.Render(points);                        
                    }
                }
                catch (Exception)
                {
                    instance?.Dispose();
                    instance = null;
                }
            }

            instance?.Dispose();
        }
#endregion

    }
}