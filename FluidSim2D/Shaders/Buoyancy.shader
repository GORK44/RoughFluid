// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "FluidSim/Buoyancy" 
{

    

	SubShader 
	{
    	Pass 
    	{
			ZTest Always

			CGPROGRAM
			#include "UnityCG.cginc"
			#pragma target 3.0
			#pragma vertex vert
			#pragma fragment frag
			
			uniform sampler2D _Velocity;
			uniform sampler2D _Temperature;
			uniform sampler2D _Density;
			uniform float _AmbientTemperature;
			uniform float _TimeStep;
			uniform float _Sigma;   //浮力
			uniform float _Kappa;   //烟重


            //float _Range;

		
			struct v2f 
			{
    			float4  pos : SV_POSITION;
    			float2  uv : TEXCOORD0;
			};

			v2f vert(appdata_base v)
			{
    			v2f OUT;
    			OUT.pos = UnityObjectToClipPos(v.vertex);
    			OUT.uv = v.texcoord.xy;
    			return OUT;
			}
			
			float4 frag(v2f IN) : COLOR
			{
			    float T = tex2D(_Temperature, IN.uv).x;
			    float2 V = tex2D(_Velocity, IN.uv).xy;
			    float D = tex2D(_Density, IN.uv).x;
			
			    float2 result = V;
			
			    if(T > _AmbientTemperature) 
			    {
			        result += (_TimeStep * (T - _AmbientTemperature) * _Sigma - D * _Kappa ) * float2(0, 1);
			    }
			    
                //
                //result += float2(_Range,0);
                //

			    return float4(result, 0, 1);
			    
			}
			
			ENDCG

    	}
	}
}