Shader "Minecraft/CloudShader" {

    Properties {
        _Color ("Color", Color) = (1, 1, 1, 1)
    }

    SubShader {

        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        LOD 100
        
        ZWrite Off
        Lighting Off
        Fog {Mode Off}

        Blend SrcAlpha OneMinusSrcAlpha

        Pass {

            Stencil {
                Ref 1
                Comp Greater
                Pass IncrSat
            }

            Color[_Color]
        }

    }
}
