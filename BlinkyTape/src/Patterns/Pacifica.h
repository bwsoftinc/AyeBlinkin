#include "../../BlinkyTape.h"

class Pacifica : public Pattern {
  private:
    uint16_t sCIStart1, sCIStart2, sCIStart3, sCIStart4;
    uint32_t sLastms;

    CRGBPalette16 pacifica_palette_1 = 
        { 0x000527, 0x000429, 0x00032B, 0x00032D, 0x000230, 0x000232, 0x000134, 0x000137, 
          0x000039, 0x00003C, 0x000056, 0x000051, 0x00005B, 0x000066, 0x14556B, 0x28AA70 };
    CRGBPalette16 pacifica_palette_2 = 
        { 0x000527, 0x000429, 0x00032B, 0x00032D, 0x000230, 0x000232, 0x000134, 0x000137, 
          0x000039, 0x00003C, 0x000046, 0x000051, 0x00005B, 0x000066, 0x0C5F72, 0x19BE7F };
    CRGBPalette16 pacifica_palette_3 = 
        { 0x000228, 0x00032E, 0x000534, 0x00063A, 0x000840, 0x000947, 0x000B4D, 0x000C53, 
          0x000E59, 0x001060, 0x001460, 0x001880, 0x001C90, 0x0020A0, 0x1040DF, 0x2060FF };

    void pacifica_deepen_colors(CRGB* leds);
    void pacifica_add_whitecaps(CRGB* leds);
    void pacifica_one_layer(CRGB* leds, CRGBPalette16& p, uint16_t cistart, uint16_t wavescale, uint8_t bri, uint16_t ioff);

  public:
    Pacifica();
    void reset();
    void draw(CRGB* leds);
};
