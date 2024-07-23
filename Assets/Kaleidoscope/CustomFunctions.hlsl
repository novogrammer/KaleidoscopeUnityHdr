#ifndef CUSTOM_FUNCTIONS_HLSL
#define CUSTOM_FUNCTIONS_HLSL


void UV2MyUV_float(float2 input,float width,float height,out float2 output){
  float aspect=width/height;
  output.x=(input.x - 0.5) * aspect + 0.5;
  output.y=input.y;
}

void MyUV2UV_float(float2 input,float width,float height,out float2 output){
  float aspect=width/height;
  output.x=(input.x - 0.5)/aspect + 0.5;
  output.y=input.y;
}

void Mirror_float(float2 input,out float2 output){
  output.x=abs(input.x - 0.5) + 0.5;
  output.y=input.y;
}


float mod_gl(float x, float y)
{
  return x - y * floor(x / y);
}
float2 mod_gl(float2 x, float2 y)
{
  return x - y * floor(x / y);
}

float len2(float2 v){
    return dot(v,v);
}

float map(float value, float min1, float max1, float min2, float max2) {
  return min2 + (value - min1) * (max2 - min2) / (max1 - min1);
}

// Get the rotation matrix from an axis and an angle (in radians)
float3x3 rotationAxisAngle( float3 v,  float a )
{
    float si = sin( a );
    float co = cos( a );
    float ic = 1.0 - co;
    return float3x3( v.x*v.x*ic + co,       v.y*v.x*ic - si*v.z,    v.z*v.x*ic + si*v.y,
                v.x*v.y*ic + si*v.z,   v.y*v.y*ic + co,        v.z*v.y*ic - si*v.x,
                v.x*v.z*ic - si*v.y,   v.y*v.z*ic + si*v.x,    v.z*v.z*ic + co );
}

float2 repeatCoordHex(float2 coord,float unitLength){
    float2 rect=float2(unitLength*3.0,sin(radians(60.0))*unitLength*2.0);
    float2 rep=mod_gl(coord,rect);
    //0 1
    // 4
    //2 3
    float2 p[5];
    p[0]=rep;
    p[1]=float2(rep.x-rect.x,rep.y);
    p[2]=float2(rep.x,rep.y-rect.y);
    p[3]=rep-rect;
    p[4]=rep-rect*0.5;
    int shortestIndex=0;
    float shortestLength=len2(p[0]);
    for(int i=1;i<5;++i){
        float l=len2(p[i]);
        if(l<shortestLength){
            shortestLength=l;
            shortestIndex=i;
        }
    }
    //return p[shortestIndex];
    if(shortestIndex==0){
        return p[0];
    }else if(shortestIndex==1){
        return p[1];
    }else if(shortestIndex==2){
        return p[2];
    }else if(shortestIndex==3){
        return p[3];
    }else{
        return p[4];
    }
}

float2 calcCoord(float2 coord){
    float l=length(coord);
    float angle=atan2(coord.y,coord.x);
    float rad60=radians(60.0);
    angle=mod_gl(angle,rad60*2.0);
    if(angle>rad60){
        angle=rad60*2.0-angle;
    }
    return float2(cos(angle),sin(angle))*l;
}

float2 rotateCoord(float2 coord,float rotation){
    
    return (mul(float3(coord,1),rotationAxisAngle(float3(0,0,1),rotation))).xy;
}

void Kaleidoscope_float(float2 input, float2 center, float unitLength, float rotation,out float2 output){
  float2 scopeCenter=float2(unitLength*0.5,unitLength*0.5/sqrt(3.0));
  output =
    rotateCoord(
      calcCoord(
        repeatCoordHex(
          rotateCoord(
            input.xy-center,
            rotation
          )+scopeCenter,
          unitLength
        )
      )-scopeCenter,
      -rotation
    )+center;
}

void CircleMask_float(float2 coordUv, float2 coordCenter, float radiusMin, float radiusMax, out float mask){
  float l=sqrt(len2(coordUv - coordCenter));
  mask=pow(1.0 - clamp(map(l,radiusMin,radiusMax,0.0,1.0),0.0,1.0),2.0);
}


// my first function
void MultiplyColor_float(float4 color, float multiplier,out float4 output)
{
    output = color * multiplier;
}

#endif // CUSTOM_FUNCTIONS_HLSL
