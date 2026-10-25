# VR Thammasat Engineering Faculty Tour

โครงการทัวร์เสมือนจริงคณะวิศวกรรมศาสตร์ มหาวิทยาลัยธรรมศาสตร์
รายวิชา CN360: Digital and Microcontroller System Development

## ทดลองบน Windows

ดาวน์โหลด [Windows x64 Prototype ZIP](https://github.com/DaybreakList/VR-Thammasat-University-Engineering-Faculty-Tour/releases/download/prototype-windows-2026-10-25/VR_TSE_Virtual_Tour_Windows_x64_2026-10-25.zip)
หรือดูรายละเอียดและ checksum ที่หน้า [Release](https://github.com/DaybreakList/VR-Thammasat-University-Engineering-Faculty-Tour/releases/tag/prototype-windows-2026-10-25)
แตกไฟล์ทั้งหมดไว้ในโฟลเดอร์เดียวกัน แล้วเปิด `VR_TSE_Virtual_Tour.exe`
ไม่ต้องติดตั้ง Unity เพื่อทดลองเล่น และต้องเก็บโฟลเดอร์ `_Data` กับไฟล์ DLL ที่แนบไว้คู่กับ EXE
อ่านวิธีควบคุมเพิ่มเติมใน [คู่มือทดลอง](Docs/TESTING.md) กด `Alt+F4` เพื่อออก

## ระบบที่พัฒนาแล้ว

- เส้นทางภาพ 360 จำลอง 10 จุด เชื่อมเป็นวงสี่เหลี่ยม เดินผ่าน Hotspot ได้ทั้งสองทิศทาง
- แผนที่แสดงตำแหน่งและเส้นทาง โดยเลือกจุดหมายจากตำแหน่งปัจจุบันได้
- เมนู MAP, DESTINATIONS, CANCEL ROUTE, HOME และ HELP
- ห้อง 301 แบบ 3D สำหรับทดลอง Teleport และกลับสู่ทางเดิน
- รองรับการทดลองด้วย XR Device Simulator บนคอม และเตรียมระบบ OpenXR สำหรับ Pico 4

ภาพสถานที่และ UX/UI ปัจจุบันเป็นแบบทดลอง จะพัฒนารูปแบบให้เหมาะกับการใช้งานจริงมากขึ้น
งานถัดไปคือทดสอบบน Pico 4 แล้วนำภาพ 360 ของสถานที่จริงมาแทนภาพจำลอง

## เปิดโปรเจกต์สำหรับทีม

1. Clone Repository นี้ แล้วเปิดโฟลเดอร์ `My project` ผ่าน Unity Hub
2. ใช้ Unity **6000.5.9f1** และเปิดใช้งาน Unity Personal ของผู้พัฒนาแต่ละคน
3. รอ Package Manager ดาวน์โหลด Dependencies และ Import ให้เสร็จ
4. เปิด `Assets/Scenes/Prototype/Floor3Prototype.unity` แล้วกด Play
5. ขณะใช้ Simulator ต้องเลือกควบคุม Controller ก่อน จึงใช้ Ray ชี้ปุ่มและกด Trigger ได้

การเปิดครั้งแรกอาจใช้เวลาหลายนาทีและต้องใช้อินเทอร์เน็ต
โฟลเดอร์ `Library` และไฟล์โครงการของ IDE จะถูกสร้างใหม่โดย Unity

## โครงสร้าง

```text
My project/
  Assets/          สคริปต์ ฉาก ภาพ โมเดล Material และไฟล์ .meta
  Packages/        รายการแพ็กเกจและ PICO SDK ฉบับแก้ความเข้ากันได้
  ProjectSettings/ การตั้งค่า Unity และ XR
Docs/              คู่มือทีม วิธีทดลอง และสถานะโครงการ
SourceAssets/Licenses/ ใบอนุญาต Asset ที่นำมาใช้
Lab_Exercise/      งานแลปเดิมของทีม
Lab_Instra360/     งานทดลองกล้องเดิมของทีม
```

ต้องเก็บไฟล์ `.meta` คู่กับ Assets และเก็บแพ็กเกจ
`My project/Packages/com.unity.xr.openxr.picoxr` ไว้ด้วย เพราะมีการแก้ไข SDK สำหรับ Unity รุ่นนี้
รายละเอียดอยู่ใน `LOCAL_PATCHES.md` ภายในแพ็กเกจ

## คู่มือและความคืบหน้า

- [คู่มือพัฒนาและ Build](Docs/DEVELOPMENT.md)
- [วิธีทดลองและรายการทดสอบ](Docs/TESTING.md)
- [สรุปสถานะโครงการ](Docs/PROJECT_STATUS.md)
- [ผังเส้นทาง 10 จุด](RECTANGULAR_LOOP.md)
- [การปรับประสิทธิภาพและผลตรวจ](OPTIMIZATION_REPORT.md)
- [เครดิตและแหล่งที่มาของ Asset](CREDITS.md)

ไฟล์ PDF รายงานและเอกสารที่ทีมเคยอัปโหลดไว้ยังอยู่ใน Repository ตามเดิม
ไฟล์ `VR_TSE_Virtual_Tour_Pico4.apk` ที่อยู่ระดับบนสุดเป็นไฟล์ที่เคยอัปโหลดไว้
อย่าใช้วันที่ของ Windows Release แทนเวอร์ชันของ APK นั้น ต้องสร้าง Android Build ใหม่ให้ตรงกับซอร์สล่าสุดก่อนทดสอบ Pico 4
