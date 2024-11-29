void read_compass() {
  HWire.beginTransmission(compass_address);                     //Start communication with the compass.
  //HWire.write(0x03);                                            //We want to start reading at the hexadecimal location 0x03.
  HWire.write(0x00); // Start at data output X MSB register
  HWire.endTransmission();                                      //End the transmission with the gyro.

  HWire.requestFrom(compass_address, 6);                        //Request 6 bytes from the compass.
  if (HWire.available() == 6) {
    // compass_y = HWire.read() << 8 | HWire.read();                 //Add the low and high byte to the compass_y variable.
    // compass_y *= -1;                                              //Invert the direction of the axis.
    // compass_z = HWire.read() << 8 | HWire.read();                 //Add the low and high byte to the compass_z variable.;
    // compass_z *= -1;                                              //Invert the direction of the axis.
    // compass_x = HWire.read() << 8 | HWire.read();                 //Add the low and high byte to the compass_x variable.;
    // //compass_x *= -1;                                              //Invert the direction of the axis.
		compass_x = (int)(int16_t)(HWire.read() | HWire.read() << 8);
    //compass_x *= -1;                                              //Invert the direction of the axis.
		compass_y = (int)(int16_t)(HWire.read() | HWire.read() << 8);
		compass_z = (int)(int16_t)(HWire.read() | HWire.read() << 8);
    compass_z *= -1;                                              //Invert the direction of the axis.
  }
  // } else {
  //   if((counter%100) == 0){
  //     Serial.println("Compass read error");
  //   }
  //   //resetCompass()
  //   failed = 1;
  //   return;
  // }

  // if((counter%100) == 0){
  //   //if(failed == 1) { Serial.print("F:   "); }
  //   Serial.print("x: " + String(compass_x) + " y: " + String(compass_y) + " z: " + String(compass_z));
  // }

  //Before the compass can give accurate measurements it needs to be calibrated. At startup the compass_offset and compass_scale
  //variables are calculated. The following part will adjust the raw compas values so they can be used for the calculation of the heading.
  if (compass_calibration_on == 0) {                            //When the compass is not beeing calibrated.
    compass_y += compass_offset_y;                              //Add the y-offset to the raw value.
    compass_y *= compass_scale_y;                               //Scale the y-value so it matches the other axis.
    compass_z += compass_offset_z;                              //Add the z-offset to the raw value.
    compass_z *= compass_scale_z;                               //Scale the z-value so it matches the other axis.
    compass_x += compass_offset_x;                              //Add the x-offset to the raw value.
  }

  //The compass values change when the roll and pitch angle of the quadcopter changes. That's the reason that the x and y values need to calculated for a virtual horizontal position.
  //The 0.0174533 value is phi/180 as the functions are in radians in stead of degrees.
  // compass_x_horizontal = (float)compass_x * cos(angle_pitch * -0.0174533) + (float)compass_y * sin(angle_roll * 0.0174533) * sin(angle_pitch * -0.0174533) - (float)compass_z * cos(angle_roll * 0.0174533) * sin(angle_pitch * -0.0174533);
  // compass_y_horizontal = (float)compass_y * cos(angle_roll * 0.0174533) + (float)compass_z * sin(angle_roll * 0.0174533);

  compass_x_horizontal = (float)compass_x * cos(angle_pitch * -0.0174533) + (float)compass_y * sin(angle_roll * 0.0174533) * sin(angle_pitch * -0.0174533) - (float)compass_z * cos(angle_roll * 0.0174533) * sin(angle_pitch * -0.0174533);
  compass_y_horizontal = (float)compass_y * cos(angle_roll * 0.0174533) + (float)compass_z * sin(angle_roll * 0.0174533);


  //Now that the horizontal values are known the heading can be calculated. With the following lines of code the heading is calculated in degrees.
  //Please note that the atan2 uses radians in stead of degrees. That is why the 180/3.14 is used.
  if (compass_y_horizontal < 0)actual_compass_heading = 180 + (180 + ((atan2(compass_y_horizontal, compass_x_horizontal)) * (180 / 3.14)));
  else actual_compass_heading = (atan2(compass_y_horizontal, compass_x_horizontal)) * (180 / 3.14);

  actual_compass_heading += declination;                                 //Add the declination to the magnetic compass heading to get the geographic north.
  if (actual_compass_heading < 0) actual_compass_heading += 360;         //If the compass heading becomes smaller then 0, 360 is added to keep it in the 0 till 360 degrees range.
  else if (actual_compass_heading >= 360) actual_compass_heading -= 360; //If the compass heading becomes larger then 360, 360 is subtracted to keep it in the 0 till 360 degrees range.
}

//At startup the registers of the compass need to be set. After that the calibration offset and scale values are calculated.
void setup_compass() {
  // HWire.beginTransmission(compass_address);
  // HWire.write(0x00); // Select configuration register A
  // HWire.write(0x78); // Sample rate: average of 8, 75Hz
  // HWire.endTransmission();

  // HWire.beginTransmission(compass_address);
  // HWire.write(0x01); // Select configuration register B
  // HWire.write(0x20); // Gain = +/- 1.3 Gauss
  // HWire.endTransmission();

  // HWire.beginTransmission(compass_address);
  // HWire.write(0x02); // Select mode register
  // HWire.write(0x00); // Continuous measurement mode
  // HWire.endTransmission();

  HWire.beginTransmission(compass_address);
  HWire.write(0x0B); // Select control register 1 (mode and settings)
  HWire.write(0x01); // Set normal mode, 8 sample average
  HWire.endTransmission();

  HWire.beginTransmission(compass_address);
  HWire.write(0x09); // Select control register 2 (range and data rate)
  HWire.write(0x1D);        // 8 sample average, 50Hz output rate, continuous mode
  HWire.endTransmission();

  //Read the calibration values from the EEPROM.
  for (int i = 0; i < 6; i++)compass_cal_values[i] = EEPROM.read(0x10 + i);

  /*
  values we got in the field
  m8n compass: -773 248 -484 506 -484 499
  qmc5883l: -1332 1440 -1733 922 -1350 1181
                 x          y         z
            -1733 922 -1350 1181 -1332 1440
                 y          z         x
  */
  // compass_cal_values[0] = -773;
  // compass_cal_values[1] = 248; // X 1,021 range
  // compass_cal_values[2] = -484;
  // compass_cal_values[3] = 506; // Y 990
  // compass_cal_values[4] = -484;
  // compass_cal_values[5] = 499; // Z 983

  compass_cal_values[0] = -1332;
  compass_cal_values[1] = 1440;
  compass_cal_values[2] = -1733;
  compass_cal_values[3] = 922;
  compass_cal_values[4] = -1350;
  compass_cal_values[5] = 1181;
  
  // compass_offset_x (248 - (-773))/2 - (248) = (1021 / 2) - 248 = 510.5 - 248 = 262.5 
  // which is kinda same thing as adding and deviding by 2.

  //Calculate the calibration offset and scale value
  compass_scale_y = ((float)compass_cal_values[1] - compass_cal_values[0]) / (compass_cal_values[3] - compass_cal_values[2]); // 1021 / 990  = 1.031313131313131
  compass_scale_z = ((float)compass_cal_values[1] - compass_cal_values[0]) / (compass_cal_values[5] - compass_cal_values[4]); // 1021 / 983  = 1.038657171922686

  compass_offset_x = ((float)compass_cal_values[1] - (float)compass_cal_values[0]) / 2 - compass_cal_values[1];
  compass_offset_y = ((float)compass_cal_values[3] - (float)compass_cal_values[2]) / 2 - compass_cal_values[3];
  compass_offset_z = ((float)compass_cal_values[5] - (float)compass_cal_values[4]) / 2 - compass_cal_values[5];
}


//The following subrouting calculates the smallest difference between two heading values.
float course_deviation(float course_b, float course_c) {
  course_a = course_b - course_c;
  if (course_a < -180 || course_a > 180) {
    if (course_c > 180)base_course_mirrored = course_c - 180;
    else base_course_mirrored = course_c + 180;
    if (course_b > 180)actual_course_mirrored = course_b - 180;
    else actual_course_mirrored = course_b + 180;
    course_a = actual_course_mirrored - base_course_mirrored;
  }
  return course_a;
}