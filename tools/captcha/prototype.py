import cv2, numpy as np, json, pathlib, time, concurrent.futures
cv2.setNumThreads(1)
ROOT=pathlib.Path(__file__).resolve().parents[2]
def foreground(image):
    rgba=cv2.imread(str(image),cv2.IMREAD_UNCHANGED)
    if rgba.shape[2]==3: return rgba
    alpha=rgba[:,:,3:4]/255.
    return np.uint8(rgba[:,:,:3]*alpha+34*(1-alpha))
def variants(include_key=False):
    choices=[]
    for path in sorted((ROOT/"Items").glob("*.webp")):
        if path.stem=="key" and not include_key:continue
        original=foreground(path)
        mask=np.max(abs(original.astype(float)-34),axis=2)>8
        y,x=np.where(mask);original=original[y.min():y.max()+1,x.min():x.max()+1]
        for aspect in [0.65,1.0,1.5]:
          stretched=cv2.resize(original,None,fx=aspect,fy=1,interpolation=cv2.INTER_LINEAR)
          for angle in range(-30,31,15):
            for length in range(12,57,4):
                scale=length/max(stretched.shape[:2])
                scaled=cv2.resize(stretched,None,fx=scale,fy=scale,interpolation=cv2.INTER_LINEAR)
                side=int(np.ceil(max(scaled.shape[:2])*1.6))+8
                canvas=np.full((side,side,3),34,np.uint8)
                top=(side-scaled.shape[0])//2;left=(side-scaled.shape[1])//2
                canvas[top:top+scaled.shape[0],left:left+scaled.shape[1]]=scaled
                matrix=cv2.getRotationMatrix2D((side/2,side/2),angle,1)
                rotated=cv2.warpAffine(canvas,matrix,(side,side),borderValue=(34,34,34))
                mask=np.max(abs(rotated.astype(float)-34),axis=2)>8
                y,x=np.where(mask)
                template=rotated[max(0,y.min()-3):y.max()+4,max(0,x.min()-3):x.max()+4]
                template=template
                gray=cv2.cvtColor(template,cv2.COLOR_BGR2GRAY)
                binary=(np.max(abs(template.astype(float)-34),axis=2)>10).astype(np.float32)
                choices.append((path.stem,gray,template,binary))
    return choices
def classify(path,choices):
    image=cv2.imread(str(path));height,width=image.shape[:2]
    # Supported attachment layouts: full 200px card or compact 85px card.
    if height>=150: crop=image[12:height-12,:min(width,145)]
    else: crop=image[:,:min(width,100)]
    crop=cv2.resize(crop,None,fx=.5,fy=.5,interpolation=cv2.INTER_AREA)
    crop=cv2.copyMakeBorder(crop,5,5,5,5,cv2.BORDER_CONSTANT,value=(34,34,34))
    crop=crop
    gray=cv2.cvtColor(crop,cv2.COLOR_BGR2GRAY)
    binary_scene=(np.max(abs(crop.astype(float)-34),axis=2)>10).astype(np.float32)
    total=binary_scene.sum()
    best={}
    for name,template,colors,binary_template in choices:
        if template.shape[0]>gray.shape[0] or template.shape[1]>gray.shape[1]:continue
        matches=cv2.matchTemplate(gray,template,cv2.TM_CCOEFF_NORMED)
        intersection=cv2.matchTemplate(binary_scene,binary_template,cv2.TM_CCORR)
        combined=.45*matches+.55*(2*intersection/(total+binary_template.sum()+1e-8))
        _,correlation,_,position=cv2.minMaxLoc(combined)
        if correlation<best.get(name,(-1,))[0]:continue
        x,y=position;patch=crop[y:y+colors.shape[0],x:x+colors.shape[1]]
        mask=(colors.max(2)-colors.min(2)>25)&(colors.max(2)>65)
        hue_score=1.
        if mask.sum()>5:
            actual=patch[mask].astype(float);reference=colors[mask].astype(float)
            if np.mean(actual.max(1)-actual.min(1))>18:
                actual-=actual.mean(1,keepdims=True);reference-=reference.mean(1,keepdims=True)
                hue_score=max(0,np.sum(actual*reference)/(np.linalg.norm(actual)*np.linalg.norm(reference)+1e-8))
        score=correlation*(.75+.25*hue_score)
        if score>best.get(name,(-1,))[0]:best[name]=(float(score),float(correlation),float(hue_score))
    return sorted(best.items(),key=lambda row:-row[1][0])[:3]
def main():
    choices=variants();records=json.loads((ROOT/"artifacts/captcha-dataset/calibration.json").read_text())
    started=time.time()
    def evaluate(record):
        ranking=classify(ROOT/"artifacts/captcha-dataset"/record["image"],choices)
        return dict(record,ranking=ranking)
    with concurrent.futures.ThreadPoolExecutor(max_workers=6) as workers: results=list(workers.map(evaluate,records))
    (ROOT/"artifacts/captcha-dataset/calibration-prototype.json").write_text("[\n"+",\n".join(json.dumps(r,separators=(",",":")) for r in results)+"\n]\n")
    errors=[(r["index"],r["expected"],r["ranking"]) for r in results if r["expected"]!=r["ranking"][0][0]]
    print("Seconds",time.time()-started,"Correct",len(results)-len(errors),"/",len(results),"Errors",errors)
if __name__=="__main__":main()
