#include "Flashlight.h"

Flashlight::Flashlight(CRGB newColor) :
  color(newColor) {
  }

void Flashlight::draw(CRGB* leds) {  
  for (uint8_t i = 0; i < LED_COUNT; i++)
    leds[i] = color;
}
