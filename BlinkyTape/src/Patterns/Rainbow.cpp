#include "Rainbow.h"

Rainbow::Rainbow() {
}

void Rainbow::reset() {
}

void Rainbow::draw(CRGB* leds) {  



  for(uint8_t i = 0; i < LED_COUNT; i++)
    leds[i] = color;

  delay(30);
}
