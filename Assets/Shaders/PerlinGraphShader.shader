Shader "Unlit Color Only" {
 
Properties {
    _Color ("Color", Color) = (1,1,1)
}
 
SubShader {
    Color [_Color]
    Pass 
    {
            Tags { "Queue"="Overlay+1" }
            ZTest Always
    }
}
 
}