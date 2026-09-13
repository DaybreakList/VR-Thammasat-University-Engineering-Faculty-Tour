from fastapi import FastAPI, Query, Response
from fastapi.responses import HTMLResponse, StreamingResponse
import cv2
import time
import threading  # 1. เพิ่ม threading
from processor import process_360_view

app = FastAPI(title="360/180 Live Streaming Server")

VIDEO_SOURCE = "sample_360.mp4" 

cap = cv2.VideoCapture(VIDEO_SOURCE)
video_lock = threading.Lock()  # 2. สร้าง Lock Object สำหรับคุม Thread

def get_live_frame():
    """ฟังก์ชันดึงเฟรมแบบ Thread-Safe"""
    global cap
    # 3. ใช้ lock ล็อคไว้ไม่ให้ Request อื่นอ่านวิดีโอพร้อมกันในวิเดียวกัน
    with video_lock:
        if not cap.isOpened():
            cap = cv2.VideoCapture(VIDEO_SOURCE)
            
        ret, frame = cap.read()
        
        if not ret and isinstance(VIDEO_SOURCE, str) and not VIDEO_SOURCE.startswith("rtsp"):
            cap.set(cv2.CAP_PROP_POS_FRAMES, 0)
            ret, frame = cap.read()
            
        # คืนค่าสำเนาของภาพเพื่อป้องกัน Memory sharing
        return ret, frame.copy() if ret and frame is not None else (False, None)

@app.get("/", response_class=HTMLResponse)
def read_index():
    with open("index.html", "r", encoding="utf-8") as f:
        return f.read()

def generate_mjpeg_stream(yaw: float, pitch: float, fov: float, mode: str):
    while True:
        ret, frame = get_live_frame()
        if not ret or frame is None:
            time.sleep(0.03)
            continue

        processed = process_360_view(frame, yaw, pitch, fov, mode)
        _, encoded_img = cv2.imencode(".jpg", processed)
        frame_bytes = encoded_img.tobytes()

        yield (b'--frame\r\n'
               b'Content-Type: image/jpeg\r\n\r\n' + frame_bytes + b'\r\n')
        
        time.sleep(0.03)

@app.get("/api/v1/stream")
def get_video_stream(
    yaw: float = Query(0.0),
    pitch: float = Query(0.0),
    fov: float = Query(90.0),
    mode: str = Query("360")
):
    return StreamingResponse(
        generate_mjpeg_stream(yaw, pitch, fov, mode),
        media_type="multipart/x-mixed-replace; boundary=frame"
    )