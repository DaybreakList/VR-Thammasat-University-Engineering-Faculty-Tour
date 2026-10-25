# คู่มือสำหรับทีมพัฒนา

## เครื่องมือ

- Git และ Unity Hub
- Unity 6000.5.9f1 พร้อม License ของแต่ละคน
- Windows Build Support สำหรับสร้าง Windows Player
- หากจะ Build สำหรับ Pico: Android Build Support, Android SDK/NDK และ OpenJDK

เปิดโฟลเดอร์ `My project` ผ่าน Unity Hub และรอ Import เสร็จก่อนเริ่มงาน
ฉากหลักคือ `Assets/Scenes/Prototype/Floor3Prototype.unity`
ไม่ต้อง Import Samples ซ้ำ เพราะไฟล์ที่โปรเจกต์ใช้ถูกเก็บไว้ใน Assets แล้ว

## Build Windows

ออกจาก Play Mode และเลือก `Tools > VR TSE > Build Windows x86-64`
ค่าเริ่มต้นจะสร้างไฟล์ใน `<repository>/Builds/Windows`
ตั้ง environment variable `VRTSE_BUILD_ROOT` ก่อนเปิด Unity ได้หากต้องการเปลี่ยนโฟลเดอร์ปลายทาง
ต้องแจกทั้งโฟลเดอร์ Build เป็น ZIP ไม่ใช่ EXE ไฟล์เดียว

## Build Pico 4

ติดตั้งโมดูล Android ของ Unity รุ่นเดียวกัน และจัดเตรียม OpenJDK 17
สคริปต์ค้นหา JDK จาก environment variable `VRTSE_JDK_ROOT` หรือ `<repository>/Tools/OpenJDK`
ตั้งตัวแปรนี้ให้ตรงกับเครื่องก่อนเปิด Unity ไม่ต้องนำ JDK/SDK/NDK ขึ้น Git
เลือก `Tools > VR TSE > Configure Pico 4 Android` แล้ว `Build Pico 4 APK`
SDK และ NDK จะอ้างอิงจากโมดูล Android ใน Unity Editor
ผลลัพธ์ปกติอยู่ที่ `<repository>/Builds/Android`

เปิด Developer Mode และ USB debugging บน Pico แล้วเชื่อมต่อ USB และยอมรับคำขอ Debugging ในแว่น

```text
adb devices
adb install -r VR_TSE_Virtual_Tour_Pico4.apk
```

ทดสอบการกด Trigger, การอ่าน UI, Teleport และประสิทธิภาพบนแว่นจริง
ผลทดสอบบนคอมไม่แทนผลทดสอบ Pico 4

## โครงสร้างข้อมูลทัวร์

`Assets/Data/TourNodes` เก็บจุดภาพ P01-P10 พร้อมตำแหน่งบนแผนที่และ Outgoing Links
แต่ละจุดมี Panorama Material ของตนเอง ส่วนจุดที่เพิ่มใหม่ยังใช้ Texture จำลองร่วมกันบางภาพ
เพิ่มจุดภาพจริงได้โดยสร้าง Node และเชื่อมเพื่อนบ้านทั้งขาไปและขากลับ
รายละเอียดอยู่ใน RECTANGULAR_LOOP.md

เมนู `Validate Rectangular Loop Routes` ตรวจเส้นทาง และ `Validate Performance Pools`
ตรวจการใช้ Hotspot/ประตูซ้ำ ทั้งสองเมนูอยู่ใต้ `Tools > VR TSE`

## ทำงานร่วมกัน

ดึงงานล่าสุดก่อนเริ่ม แยก branch ตามงาน และใช้ Pull Request สำหรับตรวจงานร่วมกัน
หลีกเลี่ยงแก้ Scene เดียวกันพร้อมกัน เพราะ Merge ยาก เก็บไฟล์ .meta ทุกครั้ง
ห้าม Commit Library, Temp, Logs, UserSettings, JDK/SDK, Token หรือ Keystore
Assets ขนาดใหญ่ให้วางแผนใช้ Git LFS ก่อน Commit; Build แจกผ่าน Releases

PICO SDK ถูกฝังใน Packages เพื่อรักษาแพตช์ความเข้ากันได้ อย่าแก้ Library/PackageCache
ใบอนุญาตของ SDK อยู่ในแพ็กเกจ และเครดิตโมเดลอยู่ใน CREDITS.md
