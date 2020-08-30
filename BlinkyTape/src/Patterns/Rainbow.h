#include "../../BlinkyTape.h"

class Rainbow : public Pattern {
  private:
    CRGB color;

  public:
    Rainbow();
    void reset();
    void draw(CRGB * leds);
};
