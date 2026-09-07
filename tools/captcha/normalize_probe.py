import os
os.environ["OPENBLAS_NUM_THREADS"]="1"
import cv2,numpy as np,json,pathlib,time
from prototype import foreground,ROOT
cv2.setNumThreads(1)
def normalize(image):
    image=cv2.medianBlur(image,3)
    mask=(np.max(abs(image.astype(float)-34),axis=2)>10).astype(np.uint8)
    opened=cv2.morphologyEx(mask,cv2.MORPH_OPEN,np.ones((5,5),np.uint8))
    count,labels,stats,centers=cv2.connectedComponentsWithStats(opened)
    if count<=1:return None
    index=max(range(1,count),key=lambda i:stats[i,4])
    x,y,w,h,area=stats[index]
    x0=max(0,x-4);y0=max(0,y-4);x1=min(image.shape[1],x+w+4);y1=min(image.shape[0],y+h+4)
    icon=cv2.resize(image[y0:y1,x0:x1],(48,48),interpolation=cv2.INTER_AREA)
    gray=cv2.cvtColor(icon,cv2.COLOR_BGR2GRAY).astype(float)-34
    gray/=np.linalg.norm(gray)+1e-8
    binary=(np.max(abs(icon.astype(float)-34),axis=2)>12).astype(float)
    return icon,gray.ravel(),binary.ravel()
def bank():
    variants=[]
    for path in sorted((ROOT/"Items").glob("*.webp")):
        original=foreground(path)
        original=cv2.resize(original,None,fx=3,fy=3,interpolation=cv2.INTER_LINEAR)
        canvas=cv2.copyMakeBorder(original,40,40,40,40,cv2.BORDER_CONSTANT,value=(34,34,34))
        h,w=canvas.shape[:2]
        for angle in range(-40,41,5):
            rotated=cv2.warpAffine(canvas,cv2.getRotationMatrix2D((w/2,h/2),angle,1),(w,h),borderValue=(34,34,34))
            features=normalize(rotated)
            if features:variants.append((path.stem,features))
    return variants
def main():
    variants=bank();refs=np.array([f[1][1] for f in variants]);masks=np.array([f[1][2] for f in variants])
    records=json.loads((ROOT/"artifacts/captcha-dataset/calibration.json").read_text());results=[]
    start=time.time()
    for record in records:
        original=cv2.imread(str(ROOT/"artifacts/captcha-dataset"/record["image"]))
        if original.shape[0]>100:original=original[15:-15,:120]
        else:original=original[:,:55]
        features=normalize(original)
        if features is None:continue
        icon,gray,binary=features
        scores=.6*(refs@gray)+.4*(2*(masks@binary)/(masks.sum(1)+binary.sum()+1e-8))
        best={}
        for i,(label,reference) in enumerate(variants):
            reficon=reference[0].astype(float);act=icon.astype(float)
            valid=(reficon.max(2)-reficon.min(2)>25)
            hue=1.
            if valid.sum()>5 and np.mean(act[valid].max(1)-act[valid].min(1))>18:
                r=reficon[valid];a=act[valid];r-=r.mean(1,keepdims=True);a-=a.mean(1,keepdims=True)
                hue=max(0,np.sum(r*a)/(np.linalg.norm(r)*np.linalg.norm(a)+1e-8))
            score=scores[i]*(.75+.25*hue)
            best[label]=max(best.get(label,0),float(score))
        ranking=sorted(best.items(),key=lambda p:-p[1])[:3]
        results.append(dict(record,ranking=ranking))
    errors=[(r["index"],r["expected"],r["ranking"]) for r in results if r["ranking"][0][0]!=r["expected"]]
    print("Seconds",time.time()-start,"Correct",len(results)-len(errors),"/",len(results),"Errors",errors)
    (ROOT/"artifacts/captcha-dataset/normalized-prototype.json").write_text("[\n"+",\n".join(json.dumps(r,separators=(",",":")) for r in results)+"\n]\n")
if __name__=="__main__":main()
