#include "Flashlight.h"

Flashlight::Flashlight(CRGB newColor) :
  color(newColor) {
  }

void Flashlight::draw(CRGB* leds) {
  fill_solid(leds, LED_COUNT, color);
  delay(30);
}
