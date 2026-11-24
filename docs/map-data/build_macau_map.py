import json, math, random
from pathlib import Path
from PIL import Image,ImageDraw,ImageFilter,ImageChops
import numpy as np
from shapely.geometry import LineString,Polygon,Point
from shapely.ops import linemerge,unary_union,nearest_points
j=json.load(open('docs/map-data/macau-osm-extract.json',encoding='utf8'));detail={'roads':j['roads'],'parks':j['parks']};j['coast']=j['coastlines']
merged=sorted(linemerge([LineString(a) for a in j['coast']]).geoms,key=lambda x:x.length,reverse=True)
land=[Polygon(list(merged[1].coords)+[(113.565,22.235),(113.60,22.27),(113.49,22.27),(113.49,22.08)])]
land += [Polygon(x).buffer(0) for x in merged if x.is_ring]
geom=unary_union(land).simplify(0.0015, preserve_topology=True)
W,H=1080,1920; xmin,xmax=113.505,113.605; ymin,ymax=22.10,22.235

def pt(p):return ((p[0]-xmin)/(xmax-xmin)*W, H-(173+(p[1]-ymin)/(ymax-ymin)*1574))
def shape(mask,poly):
 draw=ImageDraw.Draw(mask);draw.polygon([pt(x) for x in poly.exterior.coords],fill=255)
 for hole in poly.interiors:draw.polygon([pt(x) for x in hole.coords],fill=0)
positions={
 '关闸':(113.549205,22.215939),'大三巴':(113.540845,22.197455),'外港':(113.557664,22.197471),
 '氹仔':(113.556615,22.153493),'路氹':(113.5602,22.1427),'路环':(113.552522,22.117962),
 '青洲':(113.537554,22.211585),'新口岸':(113.550720,22.188997),'妈阁':(113.529413,22.183668),
 '议事亭':(113.5399,22.19365),'海洋':(113.541297,22.157843),'排角':(113.554546,22.15087),
 '澳门机场':(113.574648,22.159403),'莲花':(113.559813,22.139520),'石排湾':(113.561542,22.131421),
 '黑沙':(113.569345,22.118802),'港珠澳':(113.576074,22.200356)}

mask=Image.new('L',(W,H));polys=list(geom.geoms) if geom.geom_type=='MultiPolygon' else [geom]
for poly in polys:shape(mask,poly)
mask=mask.filter(ImageFilter.GaussianBlur(7)).point(lambda v: 255 if v >= 128 else 0)
soft=mask.filter(ImageFilter.GaussianBlur(22));a=np.asarray(mask,dtype=np.float32)/255;b=np.asarray(soft,dtype=np.float32)/255
rng=np.random.default_rng(321);noise=rng.uniform(-1,1,(H,W))*5.8;base=np.empty((H,W,3),dtype=np.uint8)
for k,(deep,shallow,ground) in enumerate([(129,182,218),(178,213,220),(192,216,186)]):base[:,:,k]=np.clip((deep+(shallow-deep)*b)*(1-a)+ground*a+noise,0,255)
# 与香港底图相同的浅色手绘地形、海面浪纹与城区笔触。
xx=np.arange(W,dtype=np.float32)[None,:]/W
vy=((H-1-np.arange(H,dtype=np.float32)[:,None])/H-0.09)/0.82
terrain=np.abs(np.sin(xx*72)+np.sin(vy*54)+np.sin((xx+vy)*41)) < 0.06
terrain &= a>0.5
for k,target in enumerate((153,163,133)):
 channel=base[:,:,k];channel[terrain]=(channel[terrain]*0.8+target*0.2).astype(np.uint8)
im=Image.fromarray(base,'RGB').convert('RGBA')
edge=ImageChops.subtract(mask.filter(ImageFilter.MaxFilter(3)),mask.filter(ImageFilter.MinFilter(3)))
edge=edge.point(lambda x: int(x*0.48))
outline=Image.new('RGBA',(W,H),(91,109,96,0)); outline.putalpha(edge); im=Image.alpha_composite(im,outline)
overlay=Image.new('RGBA',(W,H));d=ImageDraw.Draw(overlay);rnd=random.Random(23)
# 在海面画短小的蓝灰色波纹。
for _ in range(1100):
 x=rnd.randrange(2,W-14);y=rnd.randrange(2,H-2)
 if mask.getpixel((x,y)) == 0 and soft.getpixel((x,y)) < 210:
  d.line((x,y,x+rnd.randrange(4,12),y),fill=(102,148,163,36),width=1)
# 站点周边以少量淡褐色颗粒暗示城区。
for lon,lat in positions.values():
 cx,cy=pt((lon,lat));r=41
 for _ in range(90):
  ox=rnd.randrange(-r,r);oy=rnd.randrange(-r,r);x=int(cx+ox);y=int(cy+oy)
  if ox*ox+oy*oy <= r*r and 0<=x<W-1 and 0<=y<H-1 and mask.getpixel((x,y)):
   d.line((x,y,x+1,y+1),fill=(133,120,105,36),width=2)
# 山体色带与不规则浅等高线，颜色和尺度沿用香港地图。
for (lon,lat),rx,ry,seed in [((113.541,22.200),0.0035,0.003,1),((113.552,22.158),0.0045,0.0035,2),((113.565,22.124),0.0065,0.0045,3)]:
 cx,cy=pt((lon,lat));sx=rx/(xmax-xmin)*W;sy=ry/(ymax-ymin)*1574
 for k in range(4,0,-1):
  ring=[]
  for i in range(133):
   t=i/132*2*math.pi; wob=1+0.045*math.sin(t*3+seed*1.7+k)+0.02*math.sin(t*7+seed)
   ring.append((cx+math.cos(t)*sx*k*0.2*wob,cy+math.sin(t)*sy*k*0.2*wob))
  d.line(ring,fill=(120,135,104,48),width=2,joint='curve')
# 只把山体与城区等笔触叠到陆地；海浪另外绘制，避免笔触进入海面。
# 浪纹与陆地纹理已按掩膜分别限定，因此可以一起合成。
im=Image.alpha_composite(im,overlay).convert('RGB'); im.save('Assets/Resources/MapArt/macau_osm.png',optimize=True)
sc={'大三巴牌坊':(113.540845,22.197455),'妈阁庙':(113.531253,22.186092),'官也街':(113.556992,22.153556),'黑沙海滩':(113.569345,22.118802),'路环圣方济各圣堂':(113.551496,22.116904)}
for label,items in [('STATION',positions),('SCENERY',sc)]:
 for n,(lon,lat) in items.items():
  p=Point(lon,lat);out=geom.distance(p)*100000
  xy=tuple(int(t) for t in pt((lon,lat))); surface=mask.getpixel(xy) if 0<=xy[0]<W and 0<=xy[1]<H else 0
  print(label,n,'land' if surface else 'WATER', 'distance',round(out,1))

