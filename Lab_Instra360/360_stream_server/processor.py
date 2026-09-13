import cv2
import numpy as np

# ตัวแปรสำหรับ Caching จำค่าคำนวณพิกัดเดิม
_cached_params = None
_cached_maps = None

def process_360_view(img: np.ndarray, yaw: float, pitch: float, fov: float, mode: str, max_width: int = 1280) -> np.ndarray:
    global _cached_params, _cached_maps
    
    h, w = img.shape[:2]

    # 1. ย่อขนาดภาพลงมาเล็กน้อย (เช่น ความกว้างไม่เกิน 1280px) เพื่อให้ประมวลผลเร็วขึ้นมาก
    if w > max_width:
        scale = max_width / w
        img = cv2.resize(img, (int(w * scale), int(h * scale)), interpolation=cv2.INTER_NEAREST)
        h, w = img.shape[:2]

    # โหมด 180 (ใช้วิธี Roll ภาพ เร็วมากอยู่แล้ว)
    if mode == "180":
        center_x = int(((yaw + 180) % 360) / 360 * w)
        shift = (w // 2) - center_x
        rolled = np.roll(img, shift, axis=1)
        start_x = w // 4
        end_x = start_x + (w // 2)
        return rolled[:, start_x:end_x]

    # โหมด 360: ตรวจสอบว่า มุมมองเดิมหรือเปล่า?
    current_params = (yaw, pitch, fov, w, h)
    
    if _cached_params == current_params and _cached_maps is not None:
        # ถ้ามุมเดิม ใช้ค่าพิกัด map_x, map_y ที่จำไว้ได้เลย (ใช้เวลาแค่ ~2ms)
        map_x, map_y = _cached_maps
    else:
        # ถ้ามีการเลื่อน Slider ค่อยคำนวณคณิตศาสตร์ 3D ใหม่ 1 ครั้ง
        f = 0.5 * w / np.tan(np.radians(fov / 2))
        cx, cy = w / 2, h / 2

        x = np.arange(w) - cx
        y = np.arange(h) - cy
        xx, yy = np.meshgrid(x, y)

        rad_yaw = np.radians(yaw)
        rad_pitch = np.radians(pitch)

        xyz = np.stack([xx, yy, np.full_like(xx, f)], axis=-1)
        
        rot_x = np.array([
            [1, 0, 0],
            [0, np.cos(rad_pitch), -np.sin(rad_pitch)],
            [0, np.sin(rad_pitch), np.cos(rad_pitch)]
        ])
        rot_y = np.array([
            [np.cos(rad_yaw), 0, np.sin(rad_yaw)],
            [0, 1, 0],
            [-np.sin(rad_yaw), 0, np.cos(rad_yaw)]
        ])
        
        rot_xyz = xyz @ rot_x.T @ rot_y.T

        lon = np.arctan2(rot_xyz[..., 0], rot_xyz[..., 2])
        lat = np.arctan2(rot_xyz[..., 1], np.hypot(rot_xyz[..., 0], rot_xyz[..., 2]))

        map_x = ((lon / (2 * np.pi) + 0.5) * w).astype(np.float32)
        map_y = ((lat / np.pi + 0.5) * h).astype(np.float32)

        # จำค่าเก็บไว้ใน Cache
        _cached_params = current_params
        _cached_maps = (map_x, map_y)

    # ตัดภาพด้วย C++ Engine ของ OpenCV (ลื่นระดับ 30-60 FPS)
    return cv2.remap(img, map_x, map_y, cv2.INTER_LINEAR, borderMode=cv2.BORDER_WRAP)