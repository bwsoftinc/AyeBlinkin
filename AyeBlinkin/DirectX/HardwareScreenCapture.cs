using System;
using System.Threading;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Collections.Generic;
using Bitmap = System.Drawing.Bitmap;
using Rectangle = System.Drawing.Rectangle;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

using SharpDX;
using SharpDX.DXGI;
using SharpDX.Direct2D1;
using SharpDX.Direct3D11;
using SharpDX.DirectWrite;
using Factory2 = SharpDX.DXGI.Factory2;
using Resource = SharpDX.DXGI.Resource;
using Device = SharpDX.Direct3D11.Device;
using Texture = SharpDX.Direct3D11.Texture2D;
using MapFlags = SharpDX.Direct3D11.MapFlags;
using D2DAlphaMode = SharpDX.Direct2D1.AlphaMode;
using D2DPixelFormat = SharpDX.Direct2D1.PixelFormat;
using TextAntialiasMode = SharpDX.Direct2D1.TextAntialiasMode;
using TextureDescription = SharpDX.Direct3D11.Texture2DDescription;

namespace AyeBlinkin.DirectX 
{
    internal class HardwareScreenCapture : IDisposable 
    {
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
        private SolidColorBrush fpsColor;
        private static readonly int WaitTimeout = SharpDX.DXGI.ResultCode.WaitTimeout.Result.Code;
        private static readonly PresentParameters presentParameters = new PresentParameters();

        public void Dispose() 
        {
            TryReleaseFrame();
            if(Settings.SettingsHwnd != IntPtr.Zero) { // restore window background color
                device?.ImmediateContext.ClearRenderTargetView(renderTarget, SharpDX.Color.WhiteSmoke);
                renderWindow?.Present(0, PresentFlags.None);
            }

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

        private HardwareScreenCapture() { }

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

        private void Render(double fps) 
        {
            if(CheckRender(Settings.SettingsHwnd)) 
            {
                device.ImmediateContext.CopyResource(cpuTexture, renderTexture);
                renderOverlay.BeginDraw();

                renderOverlay.DrawText($"FPS: {fps:00.00}", fpsFont, fpsLocation, fpsColor);
                //TODO: draw virtual led dots in overlay
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

        private Bitmap TransferFrameCPU() 
        {
            var width = renderBounds.Width;
            var height = renderBounds.Height;

            var buffer = new Bitmap(width, height, PixelFormat.Format32bppRgb);
            var source = device.ImmediateContext.MapSubresource(cpuTexture, 0, MapMode.Read, MapFlags.None);
            var destination = buffer.LockBits(renderBounds, ImageLockMode.WriteOnly, buffer.PixelFormat);
            
            var sourcePtr = source.DataPointer;
            var destPtr = destination.Scan0;
            
            for (int i = 0; i < height; i++)
            {
                Utilities.CopyMemory(destPtr, sourcePtr, width * 4);
                sourcePtr = IntPtr.Add(sourcePtr, source.RowPitch);
                destPtr = IntPtr.Add(destPtr, destination.Stride);
            }

            buffer.UnlockBits(destination);
            device.ImmediateContext.UnmapSubresource(cpuTexture, 0);
            return buffer;
        }

        private class AvgFPSCounter
        {
            private const int framesToAverage = 30;
            private const float msScale = 1000F * framesToAverage;
            private float msTotal = 0F;
            private int dataPointer = 0;
            private float[] data = new float[framesToAverage];
            private Stopwatch timer = new Stopwatch();
            public AvgFPSCounter() => timer.Start();

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

            HardwareScreenCapture instance = null;
            var fps = new AvgFPSCounter();

            while(!token.IsCancellationRequested) 
            {
                try 
                {
                    if(instance == null) 
                    {
                        instance = new HardwareScreenCapture();
                        instance.Initialize();
                    }

                    if(instance.CaptureFrameGPU()) 
                    {
                        var buffer = instance.TransferFrameCPU();
                        
                        //TODO: transform buffer to leds
                        
                        instance.Render(fps.NextFPS());                        
                        buffer.Dispose();
                    }
                }
                catch (Exception)
                {
                    instance?.Dispose();
                    instance = null;
                }

                //TODO: throttle the FPS with sleep
            }

            instance?.Dispose();
        }
#endregion

    }
}