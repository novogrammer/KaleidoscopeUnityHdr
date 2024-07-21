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
  output.x=abs(input.x-0.5)+0.5;
  output.y=input.y;
}


void MultiplyColor_float(float4 color, float multiplier,out float4 output)
{
    output = color * multiplier;
}

#endif // CUSTOM_FUNCTIONS_HLSL
