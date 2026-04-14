import cv2
import numpy as np
import os

def remove_green_background(input_path, output_path, threshold=50):
    """
    扣除 PNG 或 JPG 图像中的绿色背景，并保存为透明 PNG。
    
    :param input_path: 输入图像路径
    :param output_path: 输出图像路径 (建议为 .png)
    :param threshold: 绿色检测的敏感度，值越小越严格
    """
    # 读取图像
    img = cv2.imread(input_path)
    if img is None:
        print(f"无法读取文件: {input_path}")
        return

    # 将 BGR 转换为 HSV 颜色空间
    hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)

    # 定义绿色的 HSV 范围
    # H: 35-85 左右通常是绿色
    lower_green = np.array([35, 40, 40])
    upper_green = np.array([85, 255, 255])

    # 创建掩码
    mask = cv2.inRange(hsv, lower_green, upper_green)

    # 反转掩码（我们要保留非绿色部分）
    mask_inv = cv2.bitwise_not(mask)

    # 将原图转换为 BGRA（带有 Alpha 通道）
    bgra = cv2.cvtColor(img, cv2.COLOR_BGR2BGRA)

    # 将掩码应用到 Alpha 通道
    # 掩码中为 0 的地方（绿色）在 Alpha 通道也会变成 0（透明）
    bgra[:, :, 3] = mask_inv

    # 保存结果
    cv2.imwrite(output_path, bgra)
    print(f"已处理并保存至: {output_path}")

if __name__ == "__main__":
    # 默认参数设置
    input_image = "input.png"  # 在此处替换你的输入文件名
    output_image = "output_no_green.png"

    if os.path.exists(input_image):
        remove_green_background(input_image, output_image)
    else:
        print(f"请确保当前目录下存在 {input_image}")
