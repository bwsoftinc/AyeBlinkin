#include "SerialLoop.h"

char patternNameBuffer[20];

void serialLoop(CRGB* leds) {
  bool playPattern = true;
  uint8_t ledIndex = 0, command0, command1, command2;
  
  while(true) {
    while(Serial.available() < COMMAND_SIZE)
      if(interruptSerialLoop)
        return;

    command0 = Serial.read();
    command1 = Serial.read();
    command2 = Serial.read();

    if(command0 != INTERRUPT) { //streaming one rgb packet for each led
      leds[ledIndex++].setRGB(command0, command1, command2);
      if(ledIndex != LED_COUNT) {
        if(ledIndex % 48 == 0)
          Serial.write(CMD_CONTINUE);
        continue;
      }
      else {
        ledIndex = 0;
        Serial.write(CMD_CONTINUE);
      }

    } else if(command1 < BRIGHTNESS) { //command1 < 0x03 is r, g, b
      for(ledIndex = 0; ledIndex < LED_COUNT; ledIndex++)
        leds[ledIndex][command1] = command2;

    } else if (command1 == BRIGHTNESS) { //return to builtin pattern not streaming or manual
      setBrightness(command2);
      if(playPattern)
        return;

    } else if (command1 == CMD_INITIALIZE) {
      sendPatterns();
      sendBrightness();
      sendPattern();
      return;

    } else if (command1 == CMD_NUMBER_LEDS) {
      setNumberLeds(command2);
      if(playPattern)
        return;    

    } else if (command1 == CMD_RESET_INDEX) {
      ledIndex = 0;
      playPattern = false;

    } else if (command1 == CMD_PATTERN) {
      if(playPattern = command2 < STREAMING_LIMIT) {
        setPattern(command2);
        return;
      }

    } else {
      while(Serial.available() > 0)
        Serial.read();

      return;
    }

    LEDS.show();
  }
}

void sendBrightness() {
  Serial.write(INTERRUPT);
  Serial.write(CMD_BRIGHTNESS);
  Serial.write(currentBrightness);
  Serial.write(INTERRUPT);
  Serial.flush();
}

void sendPattern() {
  Serial.write(INTERRUPT);
  Serial.write(CMD_PATTERN);
  Serial.write(currentPattern);
  Serial.write(INTERRUPT);
  Serial.flush();
}

void sendPatterns() {
  uint8_t i = 0;
  Serial.write(INTERRUPT);
  Serial.write(CMD_PATTERNS);
  for(;i<patternCount;i++) {
    patternStr(patternNameBuffer, i);
    Serial.write(patternNameBuffer);
    Serial.write(SERIAL_CR);
  }
  Serial.write(INTERRUPT);
  Serial.flush();
}        
