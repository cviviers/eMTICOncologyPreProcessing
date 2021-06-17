Shader "Philips/GaussianBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
		_Spread("Spread", Float) = 1.0
	}

	CGINCLUDE
	#include "UnityCG.cginc"

	static const int KernelSize = 50;
    static const int Upper = ((KernelSize - 1) / 2);
	static const int Lower = -Upper;

	sampler2D _MainTex;
	float4 _MainTex_ST;
	float2 _MainTex_TexelSize;
	float _Spread;

	static const float TWO_PI = 6.28318530718;
	static const float E = 2.71828182845904523536;

	float gaussian(int x)
	{
		float sigmaSqu = _Spread * _Spread;
		return (1 / sqrt(TWO_PI * sigmaSqu)) * pow(E, -(x * x) / (2 * sigmaSqu));
	}

	ENDCG

    SubShader
    {
        Tags 
		{ 
			"RenderType" = "Transparent"
		}

        Pass
        {
			Name "Horizontal"

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_horizontal

			float4 frag_horizontal(v2f_img i) : SV_Target
			{
				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				float kernelSum = 0.0;

				for (int x = Lower; x <= Upper; ++x)
				{
					float gauss = gaussian(x);
					kernelSum += gauss;
					col += gauss * tex2D(_MainTex, i.uv + fixed2(_MainTex_TexelSize.x * x, 0.0));
				}

				col /= kernelSum;
				return col;
			}
			ENDCG
        }

		Pass
		{
			Name "Vertical"

			CGPROGRAM
			#pragma vertex vert_img
			#pragma fragment frag_vertical

			float4 frag_vertical(v2f_img i) : SV_Target
			{
				float4 col = float4(0.0, 0.0, 0.0, 0.0);
				float kernelSum = 0.0;

				for (int y = Lower; y <= Upper; ++y)
				{
					float gauss = gaussian(y);
					kernelSum += gauss;
					col += gauss * tex2D(_MainTex, i.uv + fixed2(0.0, _MainTex_TexelSize.y * y));
				}

				col /= kernelSum;
				return col;
			}
			ENDCG
		}
    }
}
