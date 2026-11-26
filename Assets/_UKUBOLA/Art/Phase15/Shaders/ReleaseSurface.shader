Shader "UKUBOLA/ReleaseSurface" {
 Properties { _BaseMap("Albedo",2D)="white"{} _BaseColor("Tint",Color)=(1,1,1,1) _Emission("Emission",Float)=0 _Metal("Metal highlight",Range(0,1))=.2 }
 SubShader { Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }
 Pass { Tags { "LightMode"="SRPDefaultUnlit" } Cull Back ZWrite On
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #pragma target 3.5
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 float4 _BaseMap_ST; half4 _BaseColor; float _Emission; float _Metal;
 CBUFFER_END
 float4 _UK15Lights[8]; float4 _UK15Colors[8]; float4 _UK15Atmosphere; float4 _UK15Fog;
 struct A { float4 positionOS:POSITION; float3 normalOS:NORMAL; float2 uv:TEXCOORD0; };
 struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float3 normal:TEXCOORD1; float2 uv:TEXCOORD2; };
 V vert(A a){V o; o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.world=TransformObjectToWorld(a.positionOS.xyz);o.normal=TransformObjectToWorldNormal(a.normalOS);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
 half4 frag(V i):SV_Target {
  half3 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv).rgb*_BaseColor.rgb;
  float3 n=normalize(i.normal),view=SafeNormalize(_WorldSpaceCameraPos-i.world);
  float sector=i.world.z>=20?.82:(i.world.x>=20?.9:1);
  float3 light=_UK15Atmosphere.rgb*sector*(.78+.22*abs(n.y));float3 spec=0;
  [unroll] for(int j=0;j<8;j++) {float3 d=_UK15Lights[j].xyz-i.world;float len=length(d);float a=saturate(1-len/max(.01,_UK15Lights[j].w));a=a*a*_UK15Colors[j].w;float3 l=SafeNormalize(d);float diff=.18+.82*saturate(dot(n,l));light+=_UK15Colors[j].rgb*a*diff;spec+=_UK15Colors[j].rgb*a*pow(saturate(dot(n,SafeNormalize(l+view))),18)*_Metal;}
  float3 result=albedo*light+spec+albedo*_Emission;
  float fog=1-exp(-length(_WorldSpaceCameraPos-i.world)*_UK15Fog.a);
  result=lerp(result,_UK15Fog.rgb,saturate(fog));return half4(result,1);
 }
 ENDHLSL
 } }
}
