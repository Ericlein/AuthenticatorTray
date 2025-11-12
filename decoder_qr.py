from pyzbar.pyzbar import decode
from PIL import Image

# Replace with your screenshot filename
img = Image.open("img.png")

results = decode(img)
for result in results:
    data = result.data.decode("utf-8")
    print(data)
