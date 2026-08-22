//Pitch = การเอียง หน้า–หลัง (หมุนรอบแกน X)
//Roll = การเอียง ซ้าย–ขวา (หมุนรอบแกน Y)
#include <Wire.h>
#include <math.h>

#define MPU_LEFT  0x68
#define MPU_RIGHT 0x69

#define A_R 16384.0
#define G_R 131.0

struct MPUData {
  int16_t AcX, AcY, AcZ;
  int16_t GyX, GyY, GyZ;
  float pitch;
  float roll;
  float yawRate;
};

MPUData leftSensor, rightSensor;

// ประกาศ Prototype ฟังก์ชันไว้ล่วงหน้า
void wakeUp(byte addr);
void readMPU(byte addr, MPUData &m);

void setup() {
  Serial.begin(9600);
  Wire.begin();

  wakeUp(MPU_LEFT);
  wakeUp(MPU_RIGHT);

  delay(1000); // ปรับเวลา Delay ตอน setup ลงเล็กน้อยให้เริ่มต้นไวขึ้น
}

void loop() {

  readMPU(MPU_LEFT, leftSensor);
  readMPU(MPU_RIGHT, rightSensor);

  // ส่งเฉพาะ Pitch ของซ้ายและขวา
  // รูปแบบ: LeftPitch,RightPitch
  // เช่น: 69.44,83.91

  Serial.print(leftSensor.pitch, 2);
  Serial.print(",");
  Serial.println(rightSensor.pitch, 2);

  delay(20);
}

void wakeUp(byte addr)
{
  Wire.beginTransmission(addr);
  Wire.write(0x6B);
  Wire.write(0); // ปลุก MPU-6050 ให้ทำงาน
  Wire.endTransmission(true);
}

void readMPU(byte addr, MPUData &m)
{
  Wire.beginTransmission(addr);
  Wire.write(0x3B);
  Wire.endTransmission(false);

  Wire.requestFrom((uint8_t)addr, (uint8_t)14, (uint8_t)true);

  if (Wire.available() < 14)
    return;

  m.AcX = Wire.read() << 8 | Wire.read();
  m.AcY = Wire.read() << 8 | Wire.read();
  m.AcZ = Wire.read() << 8 | Wire.read();

  Wire.read(); Wire.read();   // ข้ามข้อมูลอุณหภูมิ (Temperature)

  m.GyX = Wire.read() << 8 | Wire.read();
  m.GyY = Wire.read() << 8 | Wire.read();
  m.GyZ = Wire.read() << 8 | Wire.read();

  // แปลงค่าเป็น float ก่อนคูณ เพื่อป้องกัน Overflow
  float ax = (float)m.AcX;
  float ay = (float)m.AcY;
  float az = (float)m.AcZ;

  // คำนวณ Pitch และ Roll (แก้ไขเครื่องหมายคูณ * เรียบร้อยแล้ว)
  float accPitch = atan2(ay, sqrt(ax * ax + az * az)) * 180.0 / M_PI;
  float accRoll  = atan2(-ax, sqrt(ay * ay + az * az)) * 180.0 / M_PI;

  m.pitch = accPitch;
  m.roll = accRoll;
  m.yawRate = (float)m.GyZ / G_R;
}