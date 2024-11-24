
void read_compass() {
  HWire.beginTransmission(compass_address);                     //Start communication with the compass.
  HWire.write(0x03);                                            //We want to start reading at the hexadecimal location 0x03.
  HWire.endTransmission();                                      //End the transmission with the gyro.

  HWire.beginTransmission(compass_address);
  HWire.write(0x00); // Data output register starting address
  HWire.endTransmission();

  HWire.requestFrom(compass_address, 6); // Request 6 bytes (X, Y, Z)
  if (HWire.available() == 6) {
    compass_x = (HWire.read() | (HWire.read() << 8)); // X-axis
    compass_y = (HWire.read() | (HWire.read() << 8)); // Y-axis
    compass_z = (HWire.read() | (HWire.read() << 8)); // Z-axis
  }

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
  // Initialize QMC5883L
  HWire.beginTransmission(compass_address);
  HWire.write(0x0B); // Control register 1
  HWire.write(0x01); // Set to continuous measurement mode, output rate = 10Hz, range = 2G
  HWire.endTransmission();

  HWire.beginTransmission(compass_address);
  HWire.write(0x09); // Set range register
  HWire.write(0x1D); // Full-scale range = ±8 Gauss
  HWire.endTransmission();

  HWire.beginTransmission(compass_address);
  HWire.write(0x0A); // Set oversampling register
  HWire.write(0x00); // Oversampling rate = 512
  HWire.endTransmission();

  //Read the calibration values from the EEPROM.
  for (error = 0; error < 6; error ++)compass_cal_values[error] = EEPROM.read(0x10 + error);
  error = 0;
  //Calculate the alibration offset and scale values
  compass_scale_y = ((float)compass_cal_values[1] - compass_cal_values[0]) / (compass_cal_values[3] - compass_cal_values[2]);
  compass_scale_z = ((float)compass_cal_values[1] - compass_cal_values[0]) / (compass_cal_values[5] - compass_cal_values[4]);

  compass_offset_x = (compass_cal_values[1] - compass_cal_values[0]) / 2 - compass_cal_values[1];
  compass_offset_y = (((float)compass_cal_values[3] - compass_cal_values[2]) / 2 - compass_cal_values[3]) * compass_scale_y;
  compass_offset_z = (((float)compass_cal_values[5] - compass_cal_values[4]) / 2 - compass_cal_values[5]) * compass_scale_z;
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

