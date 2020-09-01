#include "../../BlinkyTape.h"

class Rainbow : public Pattern {
  private:
    CRGB color;
    uint8_t index;
    uint8_t phase1;
    uint8_t phase2;
    uint8_t phase3;

  public:
    Rainbow(uint8_t p1, uint8_t p2, uint8_t p3);
    void reset();
    void draw(CRGB * leds);
};
