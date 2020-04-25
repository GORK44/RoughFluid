// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'


Shader "FluidSim/SubtractGradient" 
{   
    //=========================
    Properties 
    {
        _Noise1 ("Noise1 (RGB)", 2D) = "white" {}
        _Noise2 ("Noise2 (RGB)", 2D) = "white" {}
        _Noise3 ("Noise3 (RGB)", 2D) = "white" {}
        _Noise4 ("Noise4 (RGB)", 2D) = "white" {}
    }
    //=========================


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

            //=========================
            #define PI 3.1415926

            
            sampler2D _Noise1;
            sampler2D _Noise2;
            sampler2D _Noise3;
            sampler2D _Noise4;
            //=========================

			
			uniform sampler2D _Velocity;
			uniform sampler2D _Pressure;
			uniform sampler2D _Obstacles;
			uniform float _GradientScale;
			uniform float2 _InverseSize;
			
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
			


            //=======================================
            float2 randomRotate(float ran, float2 g)
            {
                float theta = ran * 2 * PI - PI; //旋转角度
                float2 gg = float2(g.x * cos(theta) - g.y * sin(theta), g.x * sin(theta) + g.y * cos(theta));  //旋转

                return gg;  //旋转后速度方向
            }
            //=======================================



			float4 frag(v2f IN) : COLOR
			{
			
			    // Find neighboring pressure:
			    float pN = tex2D(_Pressure, IN.uv + float2(0, _InverseSize.y)).x;
			    float pS = tex2D(_Pressure, IN.uv + float2(0, -_InverseSize.y)).x;
			    float pE = tex2D(_Pressure, IN.uv + float2(_InverseSize.x, 0)).x;
			    float pW = tex2D(_Pressure, IN.uv + float2(-_InverseSize.x, 0)).x;
			    float pC = tex2D(_Pressure, IN.uv).x;
			
			    // Find neighboring obstacles:
			    float bN = tex2D(_Obstacles, IN.uv + float2(0, _InverseSize.y)).x;
			    float bS = tex2D(_Obstacles, IN.uv + float2(0, -_InverseSize.y)).x;
			    float bE = tex2D(_Obstacles, IN.uv + float2(_InverseSize.x, 0)).x;
			    float bW = tex2D(_Obstacles, IN.uv + float2(-_InverseSize.x, 0)).x;
			
			    // Use center pressure for solid cells:
			    if(bN > 0.0) pN = pC;
			    if(bS > 0.0) pS = pC;
			    if(bE > 0.0) pE = pC;
			    if(bW > 0.0) pW = pC;
			
			    // Enforce the free-slip boundary condition:
			    float2 oldV = tex2D(_Velocity, IN.uv).xy;
			    float2 grad = float2(pE - pW, pN - pS) * _GradientScale;
			    



                //===========================================================================
                //改压力场梯度

                float2 g = grad;


                //4 * 3 = 12 个服从标准正态分布的随机数
                float3 r1 = tex2D(_Noise1, IN.uv).rgb;
                float3 r2 = tex2D(_Noise2, IN.uv).rgb;  
                float3 r3 = tex2D(_Noise3, IN.uv).rgb;
                float3 r4 = tex2D(_Noise4, IN.uv).rgb;


                
                // 12 个旋转后的向量
                float2 u1r = randomRotate(r1.r, g);
                float2 u1g = randomRotate(r1.g, g);
                float2 u1b = randomRotate(r1.b, g);
                
                float2 u2r = randomRotate(r2.r, g);
                float2 u2g = randomRotate(r2.g, g);
                float2 u2b = randomRotate(r2.b, g);
                
                float2 u3r = randomRotate(r3.r, g);
                float2 u3g = randomRotate(r3.g, g);
                float2 u3b = randomRotate(r3.b, g);
                
                float2 u4r = randomRotate(r4.r, g);
                float2 u4g = randomRotate(r4.g, g);
                float2 u4b = randomRotate(r4.b, g);


                //N_real取12时：
                //----------------------
                //float2 g_real = u1r + u1g + u1b + u2r + u2g + u2b + u3r + u3g + u3b + u4r + u4g + u4b;
                //g_real = g_real * 0.123;
                //----------------------

                //N_real取3时：
                //----------------------
                float2 g_real = u1r + u1g + u1b;
                g_real = g_real * 0.45;//文章第二个视频
                //g_real = g_real * 0.5;//文章第一个视频
                //----------------------




                grad = g_real;


                //===========================================================================


                
                float2 newV = oldV - grad;
			    
			    return float4(newV,0,1);  
			}
			
			ENDCG

    	}
	}
}