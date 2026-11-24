import json,math,random
from pathlib import Path
import numpy as np
from PIL import Image,ImageDraw,ImageFilter,ImageChops
j=json.load(open('Assets/Resources/MapData/guangdong_stations.json',encoding='utf8'))['stations']; W,H=1080,1920
# 无行政边界的手绘示意：以站点分布估算南侧海岸，并保留足够陆地空间。
bins=[]
for x in range(0,109,2):
 u=x/108;near=[s['y'] for s in j if abs(s['x']-u)<.09]
 bins.append((u,min(near)-.07 if near else .20))
for _ in range(2):bins=[(a,(bins[max(0,i-1)][1]+2*b+bins[min(len(bins)-1,i+1)][1])/4) for i,(a,b) in enumerate(bins)]
def pt(u,v):return (u*W,H-(173+v*1574))
mask=Image.new('L',(W,H));d=ImageDraw.Draw(mask)
poly=[(-10,-10),(W+10,-10)]+[pt(u,v) for u,v in reversed(bins)];d.polygon(poly,fill=255)
mask=mask.filter(ImageFilter.GaussianBlur(8)).point(lambda v:255 if v>127 else 0)
soft=mask.filter(ImageFilter.GaussianBlur(22));a=np.asarray(mask,dtype=np.float32)/255;b=np.asarray(soft,dtype=np.float32)/255
noise=np.random.default_rng(23).uniform(-5.5,5.5,(H,W));base=np.empty((H,W,3),dtype=np.uint8)
for k,(deep,shallow,ground) in enumerate([(129,182,218),(178,213,220),(192,216,186)]):base[:,:,k]=np.clip((deep+(shallow-deep)*b)*(1-a)+ground*a+noise,0,255)
xx=np.arange(W,dtype=np.float32)[None,:]/W;vy=((H-1-np.arange(H,dtype=np.float32)[:,None])/H-.09)/.82
terrain=(np.abs(np.sin(xx*72)+np.sin(vy*54)+np.sin((xx+vy)*41))<.06)&(a>.5)
for k,target in enumerate((153,163,133)):
 c=base[:,:,k];c[terrain]=(c[terrain]*.8+target*.2).astype(np.uint8)
im=Image.fromarray(base,'RGB').convert('RGBA');edge=ImageChops.subtract(mask.filter(ImageFilter.MaxFilter(3)),mask.filter(ImageFilter.MinFilter(3))).point(lambda x:int(x*.48))
o=Image.new('RGBA',(W,H),(91,109,96,0));o.putalpha(edge);im=Image.alpha_composite(im,o)
o=Image.new('RGBA',(W,H));d=ImageDraw.Draw(o);rng=random.Random(77)
for _ in range(1100):
 x=rng.randrange(2,W-14);y=rng.randrange(2,H-2)
 if not mask.getpixel((x,y)) and soft.getpixel((x,y))<210:d.line((x,y,x+rng.randrange(4,12),y),fill=(102,148,163,36),width=1)
for st in j:
 cx,cy=pt(st['x'],st['y'])
 for _ in range(20):
  dx=rng.randrange(-18,19);dy=rng.randrange(-18,19);x=int(cx+dx);y=int(cy+dy)
  if dx*dx+dy*dy<324 and 0<=x<W-1 and 0<=y<H-1 and mask.getpixel((x,y)):d.line((x,y,x+1,y+1),fill=(133,120,105,34),width=2)
for cx,cy,rx,ry,seed in [(.23,.67,.075,.04,1),(.49,.75,.085,.045,2),(.71,.71,.07,.04,3),(.86,.58,.07,.04,4),(.41,.54,.065,.04,5)]:
 x,y=pt(cx,cy)
 for k in range(4,0,-1):
  ring=[]
  for i in range(133):
   t=i/132*2*math.pi;wob=1+.045*math.sin(t*3+seed*1.7+k)+.02*math.sin(t*7+seed)
   ring.append((x+math.cos(t)*rx*W*k*.2*wob,y+math.sin(t)*ry*1574*k*.2*wob))
  d.line(ring,fill=(120,135,104,40),width=2,joint='curve')
im=Image.alpha_composite(im,o).convert('RGB');im.save('Assets/Resources/MapArt/guangdong_illustrated.png',optimize=True)
print('stations in water',sum(1 for p in j if not mask.getpixel(tuple(int(v) for v in pt(p['x'],p['y'])))))
