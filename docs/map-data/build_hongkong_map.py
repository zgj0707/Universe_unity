import json, math, random, re
from pathlib import Path
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageChops

W,H=1080,1920
polys=json.loads(Path('docs/map-data/hongkong-outline.json').read_text(encoding='utf8'))
def pt(u,v): return (u*W,H-(173+v*1574))
mask=Image.new('L',(W,H)); draw=ImageDraw.Draw(mask)
for poly in polys: draw.polygon([pt(u,v) for u,v in poly],fill=255)
# 同澳门底图的圆角海岸和浅海过渡，轮廓仍是游戏内原有示意位置。
mask=mask.filter(ImageFilter.GaussianBlur(12)).point(lambda x:255 if x>=128 else 0)
soft=mask.filter(ImageFilter.GaussianBlur(22)); a=np.asarray(mask,dtype=np.float32)/255; b=np.asarray(soft,dtype=np.float32)/255
rng=np.random.default_rng(321); noise=rng.uniform(-1,1,(H,W))*5.8; base=np.empty((H,W,3),dtype=np.uint8)
for k,(deep,shallow,ground) in enumerate([(129,182,218),(178,213,220),(192,216,186)]):base[:,:,k]=np.clip((deep+(shallow-deep)*b)*(1-a)+ground*a+noise,0,255)
xx=np.arange(W,dtype=np.float32)[None,:]/W;vy=((H-1-np.arange(H,dtype=np.float32)[:,None])/H-.09)/.82
terrain=(np.abs(np.sin(xx*72)+np.sin(vy*54)+np.sin((xx+vy)*41))<.06)&(a>.5)
for k,target in enumerate((153,163,133)):
 c=base[:,:,k];c[terrain]=(c[terrain]*.8+target*.2).astype(np.uint8)
im=Image.fromarray(base,'RGB').convert('RGBA')
edge=ImageChops.subtract(mask.filter(ImageFilter.MaxFilter(3)),mask.filter(ImageFilter.MinFilter(3))).point(lambda x:int(x*.48));outline=Image.new('RGBA',(W,H),(91,109,96,0));outline.putalpha(edge);im=Image.alpha_composite(im,outline)
overlay=Image.new('RGBA',(W,H));d=ImageDraw.Draw(overlay);rnd=random.Random(23)
for _ in range(1100):
 x=rnd.randrange(2,W-14);y=rnd.randrange(2,H-2)
 if not mask.getpixel((x,y)) and soft.getpixel((x,y))<210:d.line((x,y,x+rnd.randrange(4,12),y),fill=(102,148,163,36),width=1)
sea_overlay=overlay.copy()
source=Path('Assets/Scripts/DemoBootstrap.cs').read_text(encoding='utf8'); data=source[source.index('    void BuildData()'):source.index('        Add("深圳"')]
positions=re.findall(r'Add\("([^"]+)",\s*([\d.]+)f,\s*([\d.]+)f\)',data)
for name,u,v in positions:
 cx,cy=pt(float(u),float(v))
 for _ in range(90):
  dx=rnd.randrange(-41,41);dy=rnd.randrange(-41,41);x=int(cx+dx);y=int(cy+dy)
  if dx*dx+dy*dy<41*41 and 0<=x<W-1 and 0<=y<H-1 and mask.getpixel((x,y)):d.line((x,y,x+1,y+1),fill=(133,120,105,36),width=2)
for cx,cy,rx,ry,seed in [(0.53,0.258,0.085,0.05,1),(0.565,0.248,0.055,0.035,2),(0.76,0.6,0.08,0.05,3),(0.56,0.79,0.12,0.07,4),(0.62,0.8,0.075,0.05,5),(0.24,0.292,0.075,0.045,6),(0.86,0.68,0.07,0.04,7)]:
 x,y=pt(cx,cy)
 for k in range(4,0,-1):
  ring=[]
  for i in range(133):
   t=i/132*2*math.pi; wob=1+.045*math.sin(t*3+seed*1.7+k)+.02*math.sin(t*7+seed)
   ring.append((x+math.cos(t)*rx*W*k*.2*wob,y+math.sin(t)*ry*1574*k*.2*wob))
  d.line(ring,fill=(120,135,104,40),width=2,joint='curve')
# 山纹遇海裁剪；海浪另绘，使近岸保持清爽。
landmark=Image.composite(overlay,Image.new('RGBA',(W,H)),mask)
sea=Image.composite(sea_overlay,Image.new('RGBA',(W,H)),ImageChops.invert(mask))
im=Image.alpha_composite(im,landmark); im=Image.alpha_composite(im,sea).convert('RGB')
im.save('Assets/Resources/MapArt/hongkong_illustrated.png',optimize=True)
print('generated',len(positions),'original station marks')
