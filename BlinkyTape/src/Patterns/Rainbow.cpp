#include "Rainbow.h"

Rainbow::Rainbow(uint8_t p1, uint8_t p2, uint8_t p3) :
  phase1(p1),
  phase2(p2),
  phase3(p3) {
}

void Rainbow::reset() {
  index = 0;
}

void Rainbow::draw(CRGB* leds) {  
  color.r = sin8(index);
  color.g = sin8(index + 85);
  color.b = sin8(index + 170);

  for(uint8_t i = 0; i < LED_COUNT; i++)
    leds[i] = color;

  index++;
  delay(24);
}
