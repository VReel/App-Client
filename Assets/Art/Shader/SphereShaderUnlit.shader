Shader "Unlit/SphereShaderUnlit"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_FlipY ("Flip Y", Range (0,1.0)) = 1.0 // 0 is off
		_Dim ("Dim", Range (0,1.0)) = 0.0 // 0 is off
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members screenPos)
#pragma exclude_renderers d3d11
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				float4 vertex : SV_POSITION;
				float4 screenPos : TEXCOORD1;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float _FlipY;
			float _Dim;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				if (_FlipY > 0) o.uv.y = 1.0f - o.uv.y;
				o.screenPos = ComputeScreenPos(o.vertex);
				UNITY_TRANSFER_FOG(o,o.vertex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				// sample the texture
				float kScale = 2.5;
				float2 dxy = kScale * (i.screenPos.xy / i.screenPos.w - float2(0.5, 0.5)); // xy co-ordinates relative to centre
				float dxy2 = (dxy.x * dxy.x) + (dxy.y * dxy.y); // distance from centre sqrd
				float dimBrightness = (1.0-dxy2) * (1.0-_Dim); // local-dim * global-dim
				float dimFactor = lerp(1.0, dimBrightness, _Dim);
				fixed4 col = tex2D(_MainTex, i.uv) * dimFactor;

				// apply fog
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}
}
