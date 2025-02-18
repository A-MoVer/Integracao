#include <LiquidCrystal.h>

LiquidCrystal lcd(28, 30, 32, 34, 36, 38);

// Pinos
const int buttonApin = 9;    // Pisca esquerdo
const int buttonBpin = 8;    // Pisca direito
const int ledPinA    = 6;    // LED pisca esquerdo
const int ledPinB    = 5;    // LED pisca direito
const int buzzerPin  = 4;    // Buzzer

// Potenciómetros
const int accelPotPin = A0;  // Acelerador
const int brakePotPin = A1;  // Travão

// Sensor ultrassónico
const int trigPin = 50;      // Trigger
const int echoPin = 52;      // Echo

// Calibração do travão
const float brakeMinVoltage = 0.28;
const int brakeMinAnalog = int((brakeMinVoltage / 5.0) * 1023);
const float brakeMaxVoltage = 4.80;
const int brakeMaxAnalog = int((brakeMaxVoltage / 5.0) * 1023);

// Calibração do acelerador
const float accelMinVoltage = 0.22;
const int accelMinAnalog = int((accelMinVoltage / 5.0) * 1023);
const float accelMaxVoltage = 4.50;
const int accelMaxAnalog = int((accelMaxVoltage / 5.0) * 1023);

// Estados dos piscas
enum State { IDLE, BLINKING_A, BLINKING_B };
State currentState = IDLE;

// Controlo dos piscas
unsigned long lastBlinkTime = 0;
const unsigned long blinkInterval = 500;
bool ledAState = false;
bool ledBState = false;

// Velocidade
float currentSpeed = 0.0;
const float maxSpeed = 120.0;
const float brakingRate = 3.0;
const float accelSmoothing = 0.1;

// Caracteres custom para as setas
byte leftArrow[8] = {
  B00001,
  B00011,
  B00111,
  B01111,
  B00111,
  B00111,
  B00011,
  B00001
};
byte rightArrow[8] = {
  B10000,
  B11000,
  B11100,
  B11110,
  B11100,
  B11100,
  B11000,
  B10000
};

// Variáveis para debounce
const unsigned long debounceDelay = 50;
int lastButtonAReading = HIGH, lastButtonBReading = HIGH;
unsigned long lastDebounceTimeA = 0, lastDebounceTimeB = 0;
int stableButtonA = HIGH, stableButtonB = HIGH;
int lastStableButtonA = HIGH, lastStableButtonB = HIGH;

void setup() {
  pinMode(buttonApin, INPUT_PULLUP);
  pinMode(buttonBpin, INPUT_PULLUP);
  pinMode(ledPinA, OUTPUT);
  pinMode(ledPinB, OUTPUT);
  pinMode(buzzerPin, OUTPUT);
  pinMode(trigPin, OUTPUT);
  pinMode(echoPin, INPUT);
  
  digitalWrite(ledPinA, LOW);
  digitalWrite(ledPinB, LOW);
  digitalWrite(buzzerPin, LOW);

  Serial.begin(115200);
  lcd.begin(16, 2);
  lcd.createChar(0, leftArrow);
  lcd.createChar(1, rightArrow);
}

void loop() {
  // Debounce do botão A
  int readingA = digitalRead(buttonApin);
  if (readingA != lastButtonAReading) {
    lastDebounceTimeA = millis();
  }
  if (millis() - lastDebounceTimeA > debounceDelay) {
    stableButtonA = readingA;
  }
  lastButtonAReading = readingA;
  bool buttonAEvent = (stableButtonA == LOW && lastStableButtonA == HIGH);
  lastStableButtonA = stableButtonA;

  // Debounce do botão B
  int readingB = digitalRead(buttonBpin);
  if (readingB != lastButtonBReading) {
    lastDebounceTimeB = millis();
  }
  if (millis() - lastDebounceTimeB > debounceDelay) {
    stableButtonB = readingB;
  }
  lastButtonBReading = readingB;
  bool buttonBEvent = (stableButtonB == LOW && lastStableButtonB == HIGH);
  lastStableButtonB = stableButtonB;

  // Gestão dos estados dos piscas
  switch (currentState) {
    case IDLE:
      if (buttonAEvent) {
        currentState = BLINKING_A;
        ledAState = true;
        digitalWrite(ledPinA, HIGH);
        digitalWrite(ledPinB, LOW);
        ledBState = false;
        lastBlinkTime = millis();
      }
      else if (buttonBEvent) {
        currentState = BLINKING_B;
        ledBState = true;
        digitalWrite(ledPinB, HIGH);
        digitalWrite(ledPinA, LOW);
        ledAState = false;
        lastBlinkTime = millis();
      }
      break;
    case BLINKING_A:
      if (buttonAEvent) {
        currentState = IDLE;
        ledAState = false;
        digitalWrite(ledPinA, LOW);
      }
      break;
    case BLINKING_B:
      if (buttonBEvent) {
        currentState = IDLE;
        ledBState = false;
        digitalWrite(ledPinB, LOW);
      }
      break;
  }

  unsigned long now = millis();
  if (currentState == BLINKING_A) {
    if (now - lastBlinkTime >= blinkInterval) {
      lastBlinkTime = now;
      ledAState = !ledAState;
      digitalWrite(ledPinA, ledAState ? HIGH : LOW);
      if (ledAState) tone(buzzerPin, 2000, 100);
    }
  }
  else if (currentState == BLINKING_B) {
    if (now - lastBlinkTime >= blinkInterval) {
      lastBlinkTime = now;
      ledBState = !ledBState;
      digitalWrite(ledPinB, ledBState ? HIGH : LOW);
      if (ledBState) tone(buzzerPin, 2000, 100);
    }
  }
  else {
    digitalWrite(ledPinA, LOW);
    digitalWrite(ledPinB, LOW);
  }

  // Simulação de envio CAN dos piscas
  byte dataByte = 0;
  switch (currentState) {
    case IDLE:       dataByte = 0; break;
    case BLINKING_A: dataByte = 1; break;
    case BLINKING_B: dataByte = 2; break;
  }
  Serial.print("Simulando envio CAN -> ID=0x100, DATA=0x");
  if (dataByte < 0x10) Serial.print("0");
  Serial.println(dataByte, HEX);

  // Leitura e calibração do acelerador
  int accelValue = analogRead(accelPotPin);
  if (accelValue < accelMinAnalog) accelValue = accelMinAnalog;
  if (accelValue > accelMaxAnalog) accelValue = accelMaxAnalog;
  int accelPercent = map(accelValue, accelMinAnalog, accelMaxAnalog, 0, 100);
  accelPercent = constrain(accelPercent, 0, 100);
  float targetSpeed = (accelPercent / 100.0) * maxSpeed;

  Serial.print("Simulando envio CAN -> ID=0x103, ACCEL=0x");
  if (accelPercent < 0x10) Serial.print("0");
  Serial.println(accelPercent, HEX);

  // Leitura e calibração do travão
  int brakeValue = analogRead(brakePotPin);
  if (brakeValue < brakeMinAnalog) brakeValue = brakeMinAnalog;
  int brakePercent = map(brakeValue, brakeMinAnalog, brakeMaxAnalog, 0, 100);
  brakePercent = constrain(brakePercent, 0, 100);

  Serial.print("Simulando envio CAN -> ID=0x102, BRAKE=0x");
  if (brakePercent < 0x10) Serial.print("0");
  Serial.println(brakePercent, HEX);

  // Atualização da velocidade
  if (brakePercent > 5) {
    currentSpeed -= (brakePercent / 100.0) * brakingRate;
    if (currentSpeed < 0) currentSpeed = 0;
  }
  else {
    currentSpeed += (targetSpeed - currentSpeed) * accelSmoothing;
  }
  int displaySpeed = int(currentSpeed);

  Serial.print("Simulando envio CAN -> ID=0x101, SPEED=0x");
  if (displaySpeed < 0x10) Serial.print("0");
  Serial.println(displaySpeed, HEX);

  // Leitura do sensor ultrassónico
  digitalWrite(trigPin, LOW);
  delayMicroseconds(2);
  digitalWrite(trigPin, HIGH);
  delayMicroseconds(10);
  digitalWrite(trigPin, LOW);
  unsigned long duration = pulseIn(echoPin, HIGH);
  float distance = duration * 0.034 / 2;
  int distanceInt = int(distance);

  Serial.print("Simulando envio CAN -> ID=0x104, DIST=0x");
  if (distanceInt < 0x10) Serial.print("0");
  Serial.println(distanceInt, HEX);

  // Atualização do LCD
  lcd.clear();
  int leftOffset = (currentState == BLINKING_A) ? 2 : 0;
  int rightOffset = (currentState == BLINKING_B) ? 2 : 0;
  int availableColumns = 16 - leftOffset - rightOffset;
  String speedStr = String(displaySpeed) + " km/h";
  int speedLen = speedStr.length();
  int colStart = leftOffset + (availableColumns - speedLen) / 2;

  if (currentState == BLINKING_A && ledAState) {
    lcd.setCursor(0, 0);
    lcd.write(byte(0));
    lcd.write(byte(0));
  }
  if (currentState == BLINKING_B && ledBState) {
    lcd.setCursor(16 - rightOffset, 0);
    lcd.write(byte(1));
    lcd.write(byte(1));
  }
  lcd.setCursor(colStart, 0);
  lcd.print(speedStr);

  lcd.setCursor(0, 1);
  lcd.print("A:");
  if (accelPercent < 10) lcd.print("0");
  lcd.print(accelPercent);
  lcd.print("%");
  lcd.print("B:");
  if (brakePercent < 10) lcd.print("0");
  lcd.print(brakePercent);
  lcd.print("%");
  lcd.print("D:");
  if (distanceInt < 10)
    lcd.print("00");
  else if (distanceInt < 100)
    lcd.print("0");
  lcd.print(distanceInt);

  delay(100);
}
