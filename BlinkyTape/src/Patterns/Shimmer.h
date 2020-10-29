#include "../../BlinkyTape.h"

class ShimmerDot {
  private:
    bool directionForward;
    uint8_t maxValue;
    uint8_t resetDelay;
    uint16_t value;

  public:
    void reset();
    void update();
    uint8_t getValue();
};

class Shimmer : public Pattern {
  private:
    ShimmerDot shimmerDots[MAX_LEDS];
    uint8_t color_temp_factor_r;
    uint8_t color_temp_factor_g;
    uint8_t color_temp_factor_b;

  public:
    Shimmer(uint8_t r, uint8_t g, uint8_t b);
    void reset();
    void draw(CRGB* leds);  
};
