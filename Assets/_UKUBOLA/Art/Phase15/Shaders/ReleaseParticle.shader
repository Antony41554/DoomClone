Shader "UKUBOLA/ReleaseParticle" {
 Properties { _BaseMap("Particle",2D)="white"{} [HDR] _BaseColor("Color",Color)=(1,1,1,1) }
 SubShader { Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
 Pass { Tags { "LightMode"="SRPDefaultUnlit" } Cull Off ZWrite Off ZTest LEqual Blend SrcAlpha OneMinusSrcAlpha
 HLSLPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
 TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
 CBUFFER_START(UnityPerMaterial)
 half4 _BaseColor; float4 _BaseMap_ST;
 CBUFFER_END
 struct A {float4 positionOS:POSITION;float2 uv:TEXCOORD0;};struct V {float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0;};
 V vert(A a){V o;o.positionCS=TransformObjectToHClip(a.positionOS.xyz);o.uv=TRANSFORM_TEX(a.uv,_BaseMap);return o;}
 half4 frag(V i):SV_Target {return SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;}
 ENDHLSL
 } }
}
