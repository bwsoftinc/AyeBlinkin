#include "../../BlinkyTape.h"

class Plasma : public Pattern {
  private:

  public:
    Plasma();
    void reset();
    void draw(CRGB* leds);
};
