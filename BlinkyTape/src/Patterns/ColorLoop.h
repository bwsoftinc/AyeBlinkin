#include "../../BlinkyTape.h"

class ColorLoop : public Pattern {
  private:
    uint8_t rBal;
    uint8_t gBal;
    uint8_t bBal;
    
    int j;
    int f;
    int k;
    
  public:
    ColorLoop(uint8_t newRBal, uint8_t newGBal, uint8_t newBBal);
    void reset();
    void draw(CRGB* leds);
};
