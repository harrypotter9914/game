Shader "Bable/MenuLettering" {
 Properties { _MainTex("Lettering",2D)="white"{} _Tint("Ink",Color)=(1,1,1,1) _RedInk("Red ink",Float)=0 }
 SubShader { Tags { "Queue"="Transparent" "RenderType"="Transparent" } Cull Off ZWrite Off ZTest [unity_GUIZTestMode] Blend SrcAlpha OneMinusSrcAlpha
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
 struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
 sampler2D _MainTex; fixed4 _Tint; float _RedInk;
 v2f vert(appdata v) { v2f o; o.pos=UnityObjectToClipPos(v.vertex); o.uv=v.uv; o.color=v.color; return o; }
 fixed4 frag(v2f i):SV_Target { fixed3 c=tex2D(_MainTex,i.uv).rgb; float white=smoothstep(.80,.94,min(c.r,min(c.g,c.b))); float red=smoothstep(.07,.15,c.r-max(c.g,c.b)); if((i.uv.x>.455&&i.uv.y>.75)||(i.uv.x>.313&&i.uv.y<.5))red=0; float a=_RedInk>1.5?smoothstep(.22,.50,c.r):lerp(white,red,_RedInk); return fixed4(_Tint.rgb,_Tint.a*a*i.color.a); }
 ENDCG }
 }
}

