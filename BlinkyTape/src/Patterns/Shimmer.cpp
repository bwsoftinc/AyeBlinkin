#include "Shimmer.h"

// Suggested colors
// Shimmer(1, 1, 1)  // White
// Shimmer(1, 0.9352, 0.8340)  // Light champagne
// Shimmer(1, 0.9412, 0.8340)  // Medium champagne

const uint8_t valueDivisor = 4;
const uint8_t stepSize = 5;

void ShimmerDot::reset() {
    value = 0;
    maxValue = random(MAX_LEDS);
    resetDelay = random(MAX_LEDS / 4);
    direction = 1;
}

void ShimmerDot::update() {
  if (direction == 1) {
    if (value/valueDivisor < maxValue) {
      int accelerated_step = 0;
      float unit = 0;
      unit = maxValue / (float)MAX_LEDS;
      accelerated_step = (float)stepSize + ((float)MAX_LEDS * (0.015 * (float)stepSize * unit * unit * unit * unit));

      value += accelerated_step * valueDivisor;

      //error checking
      if (value/valueDivisor > MAX_LEDS)
        value -= MAX_LEDS * valueDivisor;
    }
    else
      direction = 0;
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
  return ((value/valueDivisor) &0xFF);
}

void Shimmer::reset() { 
  for (uint16_t i = 0; i < LED_COUNT; i++)
    shimmerDots[i].reset();
}

Shimmer::Shimmer(float r, float g, float b) :
  color_temp_factor_r(r),
  color_temp_factor_g(g),
  color_temp_factor_b(b) {
}

void Shimmer::draw(CRGB* leds) { 
  for (uint16_t i = 0; i < LED_COUNT; i++) {
    shimmerDots[i].update();

    leds[i].r = (uint8_t)(shimmerDots[i].getValue() * color_temp_factor_r);
    leds[i].g = (uint8_t)(shimmerDots[i].getValue() * color_temp_factor_g);
    leds[i].b = (uint8_t)(shimmerDots[i].getValue() * color_temp_factor_b);
  }
    
  delay(30);  // TODO: Don't place me here
} // end shimmer_update_loop
