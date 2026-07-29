from pathlib import Path
from math import cos, pi, sin
import random

from PIL import Image, ImageDraw, ImageFilter


ROOT = Path(__file__).resolve().parents[4]
OUT = ROOT / "HiddenWeight/Assets/Art/Fracture"
random.seed(371)

IVORY = (232, 238, 239, 255)
MARBLE = (181, 201, 207, 255)
EDGE = (89, 108, 125, 255)
MINT = (120, 235, 226, 230)
CYAN = (101, 207, 243, 245)
LAV = (183, 143, 230, 245)
VIOLET = (111, 73, 159, 235)
INK = (47, 48, 70, 255)
PALE = (241, 239, 249, 255)


def canvas(size=(1536, 1024)):
    return Image.new("RGBA", size, (0, 0, 0, 0))


def glow_layer(im, shapes, blur=18):
    layer = canvas(im.size)
    d = ImageDraw.Draw(layer)
    for shape in shapes:
        kind, coords, fill = shape
        getattr(d, kind)(coords, fill=fill)
    im.alpha_composite(layer.filter(ImageFilter.GaussianBlur(blur)))
    im.alpha_composite(layer)


def marble_block(d, box, broken=0, glow=False):
    x0, y0, x1, y1 = box
    pts = [(x0 + broken, y0), (x1 - broken // 2, y0 + broken // 3),
           (x1, y1 - broken), (x0 + broken // 3, y1)]
    d.polygon(pts, fill=IVORY, outline=EDGE, width=4)
    d.line([(x0 + 10, y0 + 14), (x1 - 12, y0 + 12)], fill=(255, 255, 255, 150), width=3)
    for i in range(3):
        xx = x0 + (i + 1) * (x1 - x0) // 4
        d.line([(xx, y0 + 8), (xx - 8, y1 - 8)], fill=(128, 157, 170, 95), width=2)
    if glow:
        d.line([(x0 + 8, y1 - 8), (x1 - 8, y1 - 8)], fill=MINT, width=5)


def flower(d, x, y, s=1.0, color=LAV):
    r = max(2, int(7 * s))
    for a in range(0, 360, 72):
        px = x + cos(a * pi / 180) * r
        py = y + sin(a * pi / 180) * r
        d.ellipse((px-r*.55, py-r*.55, px+r*.55, py+r*.55), fill=color)
    d.ellipse((x-2*s, y-2*s, x+2*s, y+2*s), fill=PALE)


def vines(d, x0, y0, length, flip=1, flowers=True):
    pts = []
    for i in range(10):
        y = y0 + i * length / 9
        x = x0 + flip * sin(i * .9) * 14
        pts.append((x, y))
        if flowers and i in (2, 5, 8):
            flower(d, x + flip * 7, y, .65)
    d.line(pts, fill=(102, 139, 128, 220), width=4)


def glass_crystal(im, x, y, s=1.0, color=MINT, crack=False):
    d = ImageDraw.Draw(im)
    pts = [(x, y-int(58*s)), (x+int(32*s), y-int(8*s)),
           (x+int(15*s), y+int(52*s)), (x-int(28*s), y+int(35*s))]
    glow_layer(im, [("polygon", pts, (color[0], color[1], color[2], 75))], int(16*s))
    d.polygon(pts, fill=(color[0], color[1], color[2], 145), outline=PALE, width=max(2, int(3*s)))
    d.line([pts[0], (x, y+int(25*s)), pts[2]], fill=(255,255,255,150), width=2)
    if crack:
        d.line([(x, y-10*s), (x-8*s, y+2*s), (x+7*s, y+14*s)], fill=VIOLET, width=3)


def arch(im, box, future=False, broken=False):
    d = ImageDraw.Draw(im)
    x0,y0,x1,y1=box
    w=x1-x0
    marble_block(d,(x0,y0+int(w*.28),x0+int(w*.18),y1),4 if broken else 0,future)
    marble_block(d,(x1-int(w*.18),y0+int(w*.28),x1,y1),6 if broken else 0,future)
    d.arc((x0,y0,x1,y0+int(w*.65)),180,360,fill=IVORY,width=max(8,int(w*.14)))
    d.arc((x0+int(w*.14),y0+int(w*.14),x1-int(w*.14),y0+int(w*.55)),
          180,360,fill=EDGE,width=3)
    for xx in (x0+15,x1-15):
        vines(d,xx,y0+int(w*.15),int((y1-y0)*.55),1 if xx==x0+15 else -1)


def save(im, rel):
    path = OUT / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    # Guarantee transparent outer corners for production cutouts.
    d = ImageDraw.Draw(im)
    for x,y in [(0,0),(im.width-10,0),(0,im.height-10),(im.width-10,im.height-10)]:
        d.rectangle((x,y,x+9,y+9), fill=(0,0,0,0))
    im.save(path)


def terrain_tiles():
    im=canvas(); d=ImageDraw.Draw(im)
    for row in range(4):
        for col in range(6):
            x=70+col*245; y=80+row*225
            marble_block(d,(x,y,x+205,y+130),broken=(row+col)%3*8,glow=row==3)
            if row==1: vines(d,x+35,y+95,95,1)
            if row==2:
                for k in range(4): flower(d,x+35+k*43,y+18,(.6+k*.08))
            if row==3:
                d.polygon([(x+40,y+130),(x+80,y+178),(x+135,y+130)],fill=(MINT[0],MINT[1],MINT[2],130))
    save(im,"Environment/Terrain/Fracture_TerrainTiles_v1.png")


def platforms():
    im=canvas(); d=ImageDraw.Draw(im)
    for i in range(12):
        col=i%3; row=i//3; x=85+col*490; y=90+row*235
        w=[290,350,405][col]
        marble_block(d,(x,y,x+w,y+55),broken=(i%4)*5,glow=i>=8)
        d.line([(x+30,y+55),(x+55,y+110),(x+w-50,y+105),(x+w-25,y+55)],fill=EDGE,width=6)
        for k in range(2+(i%4)): flower(d,x+55+k*45,y-3,.7)
    save(im,"Environment/Terrain/Fracture_Platforms_v1.png")


def platform_states():
    im=canvas((1536,768)); d=ImageDraw.Draw(im)
    for row in range(4):
        for col in range(8):
            x=20+col*192; y=30+row*192; t=col/7
            yy=y+65+int(sin(t*pi)*(-22 if row==1 else 0))
            alpha=int(255*(1-t)) if row==3 else 255
            pts=[(x+20,yy),(x+170,yy),(x+158,yy+42),(x+30,yy+42)]
            d.polygon(pts,fill=(*IVORY[:3],alpha),outline=(*EDGE[:3],alpha))
            if row==0: d.line([(x+28,yy+35),(x+162,yy+35)],fill=MINT,width=5)
            if row==2:
                crack=int(12+30*t)
                d.line([(x+96,yy),(x+88,yy+crack),(x+112,yy+42)],fill=VIOLET,width=4)
            if row==3:
                for k in range(4): d.rectangle((x+35+k*32,yy+25+k*5*t,x+48+k*32,yy+38+k*5*t),fill=(*LAV[:3],alpha))
    save(im,"Environment/Terrain/Animation/FracturePlatformStates_v1.png")


def props():
    im=canvas()
    for i in range(12):
        x=90+(i%4)*365; y=80+(i//4)*315
        d=ImageDraw.Draw(im)
        if i%4==0:
            marble_block(d,(x+45,y+80,x+205,y+210),broken=i*2)
            for k in range(5): flower(d,x+65+k*28,y+75,.65)
        elif i%4==1:
            arch(im,(x+20,y+25,x+250,y+245),future=i>5,broken=i>7)
        elif i%4==2:
            d.ellipse((x+35,y+80,x+225,y+220),fill=(112,205,215,100),outline=PALE,width=5)
            for k in range(5): glass_crystal(im,x+70+k*30,y+145+int(sin(k)*20),.35)
        else:
            d.line([(x+40,y+210),(x+230,y+210)],fill=EDGE,width=10)
            for k in range(4): vines(d,x+65+k*45,y+60,150,(-1)**k)
    save(im,"Environment/Props/Fracture_EnvironmentProps_v1.png")


def hazard_atlas():
    im=canvas(); d=ImageDraw.Draw(im)
    for row in range(3):
        for col in range(4):
            x=90+col*365; y=95+row*305
            if row==0:
                for k in range(5): glass_crystal(im,x+35+k*42,y+145,.45+(k%2)*.18, LAV if col%2 else CYAN,True)
            elif row==1:
                d.ellipse((x+30,y+75,x+250,y+190),fill=(112,222,213,60),outline=MINT,width=5)
                for k in range(7):
                    a=k*pi/3.5; glass_crystal(im,x+140+cos(a)*90,y+135+sin(a)*45,.27,CYAN,True)
            else:
                d.arc((x+25,y+40,x+250,y+230),200,340,fill=VIOLET,width=16)
                for k in range(7):
                    xx=x+40+k*30; d.polygon([(xx,y+175),(xx+14,y+115-(k%2)*30),(xx+28,y+175)],fill=LAV)
    save(im,"Environment/Hazards/Fracture_FutureHazards_v1.png")


def animated_effect(rel, mode, rows=4):
    im=canvas((1536,rows*192)); d=ImageDraw.Draw(im)
    for row in range(rows):
        for col in range(8):
            x=col*192+96; y=row*192+96; t=col/7
            if mode=="hazard":
                r=22+int(45*sin(t*pi))
                glow_layer(im,[("ellipse",(x-r,y-r,x+r,y+r),(*CYAN[:3],80))],12)
                d.ellipse((x-r,y-r,x+r,y+r),outline=CYAN,width=6)
                for k in range(3+row): d.line([(x,y),(x+cos(k*2*pi/(3+row)+t)*r*1.5,y+sin(k*2*pi/(3+row)+t)*r*1.5)],fill=LAV,width=4)
            elif mode=="foresight":
                a=int(70+185*t)
                arch(im,(x-62,y-72,x+62,y+68),future=True,broken=False)
                d.rectangle((x-70,y-80,x+70,y+75),fill=(*CYAN[:3],max(0,150-a//2)))
            elif mode=="door":
                spread=int(58*t)
                marble_block(d,(x-72-spread,y-80,x-8-spread,y+80),3,True)
                marble_block(d,(x+8+spread,y-80,x+72+spread,y+80),3,True)
            elif mode=="transit":
                ang=t*2*pi
                d.ellipse((x-62,y-62,x+62,y+62),outline=IVORY,width=10)
                d.arc((x-50,y-50,x+50,y+50),int(ang*180/pi),int(ang*180/pi)+220,fill=MINT,width=7)
                glass_crystal(im,x+cos(ang)*42,y+sin(ang)*42,.22,CYAN)
            elif mode=="ambient":
                for k in range(8):
                    xx=x-70+(k*29+col*13)%145; yy=y+65-((k*37+col*17)%130)
                    flower(d,xx,yy,.25+(k%3)*.1,(*LAV[:3],160))
            elif mode=="background":
                for k in range(4):
                    off=(col*23+k*47)%180
                    d.arc((x-90+off,y-55+k*20,x+60+off,y+50+k*20),180,350,fill=(*CYAN[:3],70),width=5)
            elif mode=="foreground":
                for k in range(3):
                    xx=x-60+k*55+int(sin(t*pi*2+k)*15)
                    vines(d,xx,y-80,150,(-1)**k,flowers=True)
    save(im,rel)


def foresight_objects():
    im=canvas()
    for i in range(12):
        x=95+(i%4)*365; y=80+(i//4)*315
        arch(im,(x+30,y+35,x+245,y+250),future=True,broken=i%3==2)
        glass_crystal(im,x+138,y+160,.45,LAV if i%2 else MINT,crack=i>7)
    save(im,"Environment/Interactables/Fracture_ForesightObjects_v1.png")


def doors():
    im=canvas()
    for i in range(8):
        x=85+(i%4)*370; y=65+(i//4)*475
        arch(im,(x,y,x+275,y+365),future=i%2==0,broken=i in (3,7))
        d=ImageDraw.Draw(im)
        d.ellipse((x+95,y+145,x+180,y+230),fill=(CYAN[0],CYAN[1],CYAN[2],80),outline=LAV,width=4)
    save(im,"Environment/Interactables/Fracture_DoorsShortcuts_v1.png")


def transit_structures():
    im=canvas(); d=ImageDraw.Draw(im)
    for i in range(8):
        x=85+(i%4)*370; y=80+(i//4)*460
        if i%2==0:
            d.ellipse((x+30,y+55,x+280,y+305),outline=IVORY,width=18)
            d.ellipse((x+65,y+90,x+245,y+270),outline=MINT,width=8)
            for k in range(6): flower(d,x+155+cos(k*pi/3)*115,y+180+sin(k*pi/3)*115,.6)
        else:
            marble_block(d,(x+40,y+230,x+270,y+290),8,True)
            d.polygon([(x+80,y+230),(x+155,y+60),(x+230,y+230)],fill=(CYAN[0],CYAN[1],CYAN[2],80),outline=PALE)
            glass_crystal(im,x+155,y+165,.65,LAV)
    save(im,"Environment/Interactables/Fracture_TransitStructures_v1.png")


def ambient_static():
    im=canvas(); d=ImageDraw.Draw(im)
    for i in range(36):
        x=65+(i%9)*165; y=60+(i//9)*235
        if i%4==0: glass_crystal(im,x,y,.28+(i%3)*.08,CYAN)
        elif i%4==1:
            for k in range(7): flower(d,x+cos(k)*30,y+sin(k)*30,.35)
        elif i%4==2:
            d.arc((x-45,y-45,x+45,y+45),30,300,fill=MINT,width=5)
            d.ellipse((x-8,y-8,x+8,y+8),fill=PALE)
        else:
            d.line([(x-40,y+35),(x+40,y-35)],fill=(*LAV[:3],160),width=4)
            d.line([(x-40,y-35),(x+40,y+35)],fill=(*CYAN[:3],140),width=3)
    save(im,"Environment/VFX/Fracture_AmbientVFX_v1.png")


def collectible_transitions():
    im=canvas((1536,768)); d=ImageDraw.Draw(im)
    for row in range(4):
        for col in range(8):
            x=96+col*192; y=96+row*192; t=col/7
            s=.25+.5*sin(t*pi)
            if row==0: glass_crystal(im,x,y,s,CYAN)
            elif row==1:
                flower(d,x,y,s*1.5,LAV)
                d.ellipse((x-30*t,y-30*t,x+30*t,y+30*t),outline=(*MINT[:3],int(220*(1-t))),width=4)
            elif row==2:
                d.ellipse((x-24,y-24,x+24,y+24),fill=(*LAV[:3],200),outline=PALE,width=3)
                d.arc((x-55,y-55,x+55,y+55),int(t*360),int(t*360)+210,fill=CYAN,width=5)
            else:
                for k in range(7):
                    a=k*2*pi/7+t*pi; rr=50*t
                    flower(d,x+cos(a)*rr,y+sin(a)*rr,.3,(*LAV[:3],int(255*(1-t))))
    save(im,"Gameplay/Items/Animation/FractureCollectibleTransitions_v1.png")


def projectile_sheet(rel, boss=False):
    rows=4; im=canvas((1536,768)); d=ImageDraw.Draw(im)
    for row in range(rows):
        for col in range(8):
            x=col*192+96; y=row*192+96; t=col/7
            if boss and row==0:
                d.polygon([(x-70,y),(x,y-24-int(t*12)),(x+72,y),(x,y+24+int(t*12))],fill=(*LAV[:3],180),outline=PALE)
            elif row==1:
                for k in range(3+(2 if boss else 0)):
                    a=k*2*pi/(3+(2 if boss else 0))+t
                    glass_crystal(im,x+cos(a)*45,y+sin(a)*45,.2,CYAN)
            elif row==2:
                d.arc((x-55,y-55,x+55,y+55),int(t*360),int(t*360)+260,fill=LAV,width=9)
                d.ellipse((x-14,y-14,x+14,y+14),fill=MINT)
            else:
                r=int(12+50*t)
                d.ellipse((x-r,y-r,x+r,y+r),outline=(*CYAN[:3],int(255*(1-t))),width=6)
                for k in range(5): flower(d,x+cos(k*1.25)*r,y+sin(k*1.25)*r,.25)
    save(im,rel)


def impact_sheet():
    im=canvas((1536,768)); d=ImageDraw.Draw(im)
    for row in range(4):
        for col in range(8):
            x=col*192+96; y=row*192+96; t=col/7; r=12+int(t*65)
            color=[CYAN,LAV,IVORY,VIOLET][row]
            for k in range(8):
                a=k*pi/4+row*.2
                p0=(x+cos(a)*r*.25,y+sin(a)*r*.25)
                p1=(x+cos(a)*r,y+sin(a)*r)
                d.line([p0,p1],fill=(*color[:3],int(255*(1-t*.75))),width=max(2,8-col//2))
            d.ellipse((x-r*.25,y-r*.25,x+r*.25,y+r*.25),fill=(*PALE[:3],int(240*(1-t))))
    save(im,"Gameplay/VFX/Animation/FractureImpactVFX_v1.png")


def secondary_vfx():
    im=canvas(); d=ImageDraw.Draw(im)
    for i in range(24):
        x=85+(i%6)*245; y=85+(i//6)*230
        kind=i%4
        if kind==0:
            d.arc((x-55,y-55,x+55,y+55),20,330,fill=CYAN,width=8)
            d.arc((x-38,y-38,x+38,y+38),190,500,fill=LAV,width=5)
        elif kind==1:
            for k in range(8): d.line([(x,y),(x+cos(k*pi/4)*60,y+sin(k*pi/4)*60)],fill=LAV,width=5)
        elif kind==2:
            for k in range(6): flower(d,x+cos(k*pi/3)*45,y+sin(k*pi/3)*45,.5)
        else:
            d.polygon([(x,y-65),(x+22,y-12),(x+55,y+40),(x,y+20),(x-55,y+40),(x-22,y-12)],fill=(*MINT[:3],110),outline=PALE)
    save(im,"Gameplay/VFX/FractureSecondaryVFX_v1.png")


def room_transitions():
    im=canvas((1536,768)); d=ImageDraw.Draw(im)
    for row in range(4):
        for col in range(8):
            x=col*192; y=row*192; t=col/7
            a=int(220*sin(t*pi))
            if row==0:
                d.rectangle((x+20,y+20,x+172,y+172),outline=(*CYAN[:3],a),width=10)
                for k in range(5): flower(d,x+96+cos(k)*55*t,y+96+sin(k)*55*t,.35,(*LAV[:3],a))
            elif row==1:
                for k in range(7):
                    xx=x+20+k*25; d.line([(xx,y+170),(x+96,y+20)],fill=(*IVORY[:3],a),width=5)
            elif row==2:
                d.ellipse((x+20,y+20,x+172,y+172),outline=(*LAV[:3],a),width=12)
                d.ellipse((x+65,y+65,x+127,y+127),fill=(*CYAN[:3],a//2))
            else:
                d.polygon([(x+96,y+10),(x+180,y+96),(x+96,y+182),(x+12,y+96)],outline=(*MINT[:3],a),width=9)
    save(im,"Environment/Interactables/Animation/FractureRoomTransitions_v1.png")


def ui_icons():
    im=canvas((1024,1024)); d=ImageDraw.Draw(im)
    for i in range(25):
        x=52+(i%5)*198; y=52+(i//5)*198
        d.ellipse((x,y,x+128,y+128),fill=(37,42,62,220),outline=IVORY,width=5)
        kind=i%5
        if kind==0: glass_crystal(im,x+64,y+64,.45,CYAN,crack=i>14)
        elif kind==1:
            d.arc((x+25,y+25,x+103,y+103),30,325,fill=LAV,width=10)
            d.polygon([(x+83,y+18),(x+106,y+35),(x+78,y+44)],fill=PALE)
        elif kind==2:
            flower(d,x+64,y+64,2.4,LAV)
        elif kind==3:
            d.polygon([(x+64,y+18),(x+108,y+64),(x+64,y+110),(x+20,y+64)],fill=(*MINT[:3],120),outline=PALE)
        else:
            d.line([(x+28,y+96),(x+64,y+28),(x+100,y+96)],fill=CYAN,width=9)
            d.ellipse((x+53,y+73,x+75,y+95),fill=LAV)
    save(im,"UI/FractureUIIcons_v1.png")


def status_ui():
    im=canvas((1536,768)); d=ImageDraw.Draw(im)
    for row in range(4):
        for col in range(8):
            x=col*192+96; y=row*192+96; t=col/7
            d.ellipse((x-60,y-60,x+60,y+60),outline=(*IVORY[:3],190),width=5)
            if row==0:
                d.arc((x-50,y-50,x+50,y+50),-90,-90+int(360*t),fill=MINT,width=10)
            elif row==1:
                for k in range(col+1): flower(d,x-42+(k%4)*28,y-14+(k//4)*28,.35)
            elif row==2:
                d.line([(x-40,y),(x+40,y)],fill=LAV,width=9)
                d.line([(x,y-40),(x,y+40)],fill=LAV,width=9)
                d.ellipse((x-10-col*3,y-10-col*3,x+10+col*3,y+10+col*3),outline=CYAN,width=4)
            else:
                for k in range(6):
                    a=k*pi/3+t*pi; d.ellipse((x+cos(a)*38-6,y+sin(a)*38-6,x+cos(a)*38+6,y+sin(a)*38+6),fill=CYAN)
    save(im,"UI/Animation/FractureStatusUI_v1.png")


def main():
    terrain_tiles(); platforms(); platform_states(); props()
    hazard_atlas()
    animated_effect("Environment/Hazards/Animation/FutureHazardTransitions_v1.png","hazard")
    foresight_objects()
    animated_effect("Environment/Interactables/Animation/ForesightObjectTransitions_v1.png","foresight")
    doors()
    animated_effect("Environment/Interactables/Animation/DoorShortcutTransitions_v1.png","door")
    transit_structures()
    animated_effect("Environment/Interactables/Animation/TransitTransitions_v1.png","transit")
    ambient_static()
    animated_effect("Environment/VFX/Animation/FractureAmbientMotion_v1.png","ambient")
    animated_effect("Environment/VFX/Animation/FractureBackgroundMotion_v1.png","background",3)
    animated_effect("Environment/VFX/Animation/FractureForegroundMotion_v1.png","foreground",3)
    collectible_transitions()
    projectile_sheet("Gameplay/VFX/Animation/FractureEnemyProjectiles_v1.png",False)
    projectile_sheet("Gameplay/VFX/Animation/FractureBossProjectiles_v1.png",True)
    impact_sheet(); secondary_vfx(); room_transitions(); ui_icons(); status_ui()
    print("Generated 24 Fracture gameplay atlases.")


if __name__ == "__main__":
    main()
