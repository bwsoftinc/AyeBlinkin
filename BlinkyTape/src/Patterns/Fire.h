#include "../../BlinkyTape.h"

#define HEIGHT 20

class Fire : public Pattern {
  private:
    uint8_t SPARKING;
    uint8_t COOLING;
    uint8_t heat[2*HEIGHT];

  public:
    Fire(uint8_t sparking, uint8_t cooling);
    void reset();
    void draw(CRGB * leds);
};
