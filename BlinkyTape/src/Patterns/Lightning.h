#include "../../BlinkyTape.h"

class Lightning : public Pattern {
  private:
    bool flash;
    uint8_t startIndex;
    uint8_t endIndex;
    uint8_t flashes;
    uint8_t frequency;
    uint8_t flashLength;
    uint8_t flashCounter;
    uint8_t level;
    int16_t delayCount;

  public:
    Lightning(uint8_t flashes, uint8_t frequency);
    void reset();
    void draw(CRGB* leds);
};
