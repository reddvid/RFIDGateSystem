void setup() {
  pinMode(7, INPUT);
  pinMode(10, OUTPUT);
  Serial.begin(9600);
}

void loop() {
  int val = 0;
  int var;
  var = 0;
  
  if (Serial.available()) { //start read serial
    delay(100); //delay after read
    val = Serial.read(); //reads '1' or '0' from serial
    
    if (val == '1' & digitalRead(7) == LOW) { //if read is '1' then miniature switch is LOW-indicating a closed gate-s
      digitalWrite(10, HIGH);
      delay(500);
      digitalWrite(10, LOW); //sends signal which opens the gate
      delay(10000); //safety delay timeframe for opening of gate takes 5 seconds
     
      if (digitalRead(7) == HIGH) { //reads if miniature is pressed it is HIGH-indicating the gate is open
        delay(5000); //additional delay timeframe for the car to pass
        digitalWrite(10, HIGH);
        delay(500); //sends signal to open
        digitalWrite(10, LOW);
        delay(5000);
       
        if (digitalRead(7) == HIGH) { //if again if gate is obstructed FIRST
          digitalWrite(10, HIGH);
          delay(500);
          digitalWrite(10, LOW);
          delay(3000); //safety delay
        
          if (digitalRead(7) == HIGH) { //if again if gate is obstructed TWICE
            digitalWrite(10, HIGH);
            delay(500);
            digitalWrite(10, LOW);
            delay(3000);
          
            if (digitalRead(7) == HIGH) { //if again if gate is obstructed THRICE (MAX)
              digitalWrite(10, HIGH);
              delay(500);
              digitalWrite(10, LOW);
              delay(3000);
            }
          }
        }
      }
    }
  }
}
