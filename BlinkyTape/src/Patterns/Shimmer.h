#include "../../BlinkyTape.h"

class ShimmerDot {
  private:
    bool directionForward;
    uint8_t maxValue;
    uint8_t resetDelay;
    int value;

  public:
    void reset();
    void update();
    uint8_t getValue();
};

class Shimmer : public Pattern {
  private:
    ShimmerDot shimmerDots[MAX_LEDS];
    float color_temp_factor_r;
    float color_temp_factor_g;
    float color_temp_factor_b;

  public:
    Shimmer(float r, float g, float b);
    void reset();
    void draw(CRGB* leds);  
};
