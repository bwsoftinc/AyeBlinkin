#include "Shimmer.h"

// Suggested colors
// Shimmer(1, 1, 1)  // White
// Shimmer(1, 0.9352, 0.8340)  // Light champagne
// Shimmer(1, 0.9412, 0.8340)  // Medium champagne

const uint8_t valueDivisor = 4;
const uint8_t stepSize = 5;

void ShimmerDot::reset() {
    value = 0;
    maxValue = random8(MAX_LEDS);
    resetDelay = random8(MAX_LEDS / 4);
    directionForward = true;
}

void ShimmerDot::update() {
  if (directionForward) {
    if (value/valueDivisor < maxValue) {
      int accelerated_step = 0;
      float unit = 0;
      unit = maxValue / (float)MAX_LEDS;
      accelerated_step = (float)stepSize + ((float)MAX_LEDS * (0.015 * (float)stepSize * unit * unit * unit * unit));

      value += accelerated_step * valueDivisor;

      //error checking
      if (value/valueDivisor > MAX_LEDS)
        value -= LED_COUNT * valueDivisor;
    }
    else
      directionForward = false;
  }
  else {
    if (value/valueDivisor > 0) {
      value = value - stepSize * valueDivisor;

      //error checking
      if (value < 0)
        value = 0;
    }
    else {
      if(resetDelay == 0)
        reset();

      resetDelay--;
    }
  }
}

uint8_t ShimmerDot::getValue() {
  return ((value/valueDivisor) & 0xFF);
}

void Shimmer::reset() { 
  for (uint8_t i = 0; i < MAX_LEDS; i++)
    shimmerDots[i].reset();
}

Shimmer::Shimmer(uint8_t r, uint8_t g, uint8_t b) :
  color_temp_factor_r(r),
  color_temp_factor_g(g),
  color_temp_factor_b(b) {
}

void Shimmer::draw(CRGB* leds) { 
  for (uint8_t i = 0; i < MAX_LEDS; i++) {
    shimmerDots[i].update();

    leds[i].r = (uint8_t)(shimmerDots[i].getValue() * color_temp_factor_r / 255);
    leds[i].g = (uint8_t)(shimmerDots[i].getValue() * color_temp_factor_g / 255);
    leds[i].b = (uint8_t)(shimmerDots[i].getValue() * color_temp_factor_b / 255);
  }
    
  delay(30);
}
