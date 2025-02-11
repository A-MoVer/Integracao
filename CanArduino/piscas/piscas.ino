const int buttonApin = 9;
const int buttonBpin = 8;
const int ledPinA    = 6;
const int ledPinB    = 5;

const int PinCLK = 48;
const int PinDT  = 50;
const int PinSW  = 52;

int encoderValue = 0;
int lastCLK;


enum State {
  IDLE,
  BLINKING_A,
  BLINKING_B
};

State currentState = IDLE; 

unsigned long lastBlinkTime = 0;  
const unsigned long blinkInterval = 500; 

bool ledAState = false;
bool ledBState = false;




void setup() {
  pinMode(buttonApin, INPUT_PULLUP);
  pinMode(buttonBpin, INPUT_PULLUP);
  pinMode(ledPinA, OUTPUT);
  pinMode(ledPinB, OUTPUT);
  pinMode(PinCLK, INPUT_PULLUP);
  pinMode(PinDT,  INPUT_PULLUP);
  pinMode(PinSW,  INPUT_PULLUP);

  lastCLK = digitalRead(PinCLK);

  digitalWrite(ledPinA, LOW);
  digitalWrite(ledPinB, LOW);


  Serial.begin(115200);
  lastCLK = digitalRead(PinCLK); 
}


void loop() {
  bool buttonA = (digitalRead(buttonApin) == LOW); 
  bool buttonB = (digitalRead(buttonBpin) == LOW);

  switch (currentState)
  {
    case IDLE:
      if (buttonA) 
      {
        currentState = BLINKING_A;
        ledAState = true;               
        digitalWrite(ledPinA, ledAState);
        digitalWrite(ledPinB, LOW);
        ledBState = false;
        lastBlinkTime = millis();
      } 
      else if (buttonB) 
      {
        currentState = BLINKING_B;
        ledBState = true;
        digitalWrite(ledPinB, ledBState);
        digitalWrite(ledPinA, LOW);
        ledAState = false;
        lastBlinkTime = millis();
      }
      break;

    case BLINKING_A:
      if (buttonA)
      {
        currentState = IDLE;
        ledAState = false;
        digitalWrite(ledPinA, LOW);
      }
      break;

    case BLINKING_B:
      if (buttonB) {
        currentState = IDLE;
        ledBState = false;
        digitalWrite(ledPinB, LOW);
      }
      break;
  }

  if (currentState == BLINKING_A) {
    unsigned long now = millis();
    if ((now - lastBlinkTime) >= blinkInterval) {
      lastBlinkTime = now;
      ledAState = !ledAState;
      digitalWrite(ledPinA, ledAState ? HIGH : LOW);
    }
  } 
  else if (currentState == BLINKING_B) {
    unsigned long now = millis();
    if ((now - lastBlinkTime) >= blinkInterval) {
      lastBlinkTime = now;
      ledBState = !ledBState;
      digitalWrite(ledPinB, ledBState ? HIGH : LOW);
    }
  } 
  else {
    digitalWrite(ledPinA, LOW);
    digitalWrite(ledPinB, LOW);
  }


  byte dataByte = 0;
  switch (currentState) {
    case IDLE:        dataByte = 0; break;
    case BLINKING_A:  dataByte = 1; break;
    case BLINKING_B:  dataByte = 2; break;
  }

  
  Serial.print("Simulando envio CAN -> ID=0x100, DATA=0x");
  if (dataByte < 0x10) Serial.print("0");
  Serial.println(dataByte, HEX);

  int stateCLK = digitalRead(PinCLK);
  if (stateCLK != lastCLK) {
    if (digitalRead(PinDT) != stateCLK) {
      encoderValue++;
    } else {
      encoderValue--;
    }
  }
  lastCLK = stateCLK;

  if (digitalRead(PinSW) == LOW) {
    encoderValue = 0;
    Serial.println("Reset do encoder!");
  }


  int speedValue = encoderValue;
  if (speedValue < 0)   speedValue = 0;
  if (speedValue > 120) speedValue = 120;



  Serial.print("Simulando envio CAN -> ID=0x101, SPEED=0x");
  if (speedValue < 0x10) Serial.print("0");
  Serial.println(speedValue, HEX);

}
