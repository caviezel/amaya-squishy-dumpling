using System;
using System.Collections.Generic;
using UnityEngine;
namespace SquishyDumpling.Creation
{
    // Topology independent. No Unity physics, transform animation, tween, or allocation in Advance.
    public sealed class DumplingSpringSimulation
    {
        public readonly Vector3[] Rest, Positions, Velocities;
        public readonly int[] Triangles;
        readonly Vector3[] forces, offsets, grabRest;
        readonly int[] neighbors, degree;
        readonly DumplingDeformationConfiguration config;
        float accumulator;
        Vector2 contact, initial, pointer;
        public bool Held { get; private set; }
        public float MaximumDisplacement { get; private set; }
        public float MaximumSpeed { get; private set; }
        public float Strain { get; private set; }
        public DumplingSpringSimulation(Vector3[] rest,int[] triangles,DumplingDeformationConfiguration configuration)
        {
            config=configuration; Rest=(Vector3[])rest.Clone(); Positions=(Vector3[])rest.Clone(); Triangles=(int[])triangles.Clone();
            Velocities=new Vector3[rest.Length]; forces=new Vector3[rest.Length]; offsets=new Vector3[rest.Length]; degree=new int[rest.Length]; grabRest=new Vector3[rest.Length];
            var edges=new HashSet<ulong>(); var list=new List<int>();
            for(int i=0;i<triangles.Length;i+=3) for(int k=0;k<3;k++)
            {
                int a=triangles[i+k], b=triangles[i+(k+1)%3]; if(a>b) { int t=a;a=b;b=t; }
                ulong key=((ulong)(uint)a<<32)|(uint)b;
                if(edges.Add(key)) { list.Add(a); list.Add(b); degree[a]++;degree[b]++; }
            }
            neighbors=list.ToArray();
        }
        public void Begin(Vector2 restContact,Vector2 localPointer)
        { contact=restContact; initial=pointer=localPointer; Held=true; for(int i=0;i<Rest.Length;i++) grabRest[i]=Positions[i]-Rest[i]; }
        public void Move(Vector2 localPointer) { if(Finite(localPointer)) pointer=localPointer; }
        public void Release() { Held=false; }
        public void Reset()
        {
            Held=false; contact=initial=pointer=Vector2.zero; accumulator=0; MaximumDisplacement=MaximumSpeed=Strain=0;
            Array.Copy(Rest,Positions,Rest.Length); Array.Clear(Velocities,0,Velocities.Length); Array.Clear(forces,0,forces.Length); Array.Clear(offsets,0,offsets.Length); Array.Clear(grabRest,0,grabRest.Length);
        }
        public void Advance(float elapsed)
        {
            if(float.IsNaN(elapsed)||float.IsInfinity(elapsed)||elapsed<=0) return;
            float dt=DumplingDeformationConfiguration.Safe(config.fixedStep,1f/240,.002f,.01f);
            accumulator=Mathf.Min(accumulator+elapsed,dt*Mathf.Clamp(config.maxSubsteps,4,32));
            while(accumulator>=dt) { Integrate(dt); accumulator-=dt; }
        }
        void Integrate(float dt)
        {
            Vector2 drag=Vector2.ClampMagnitude(pointer-initial,1000);
            float max=config.SafeMaxStretch, d=drag.magnitude;
            // Smooth asymptotic saturation: increasing resistance BEFORE the safety constraints.
            float p=config.ResistanceExponent;
            // Equivalent bounded-norm form avoids pow overflow for extreme targets/exponents.
            Vector2 resisted=d>max ? drag/d*(max/Mathf.Pow(1+Mathf.Pow(max/d,p),1/p)) : drag/Mathf.Pow(1+Mathf.Pow(d/max,p),1/p);
            float stiffness=DumplingDeformationConfiguration.Safe(config.springStiffness,65,.1f,20000);
            float recovery=DumplingDeformationConfiguration.Safe(config.recoveryStrength,1,.01f,16);
            float grabStrength=DumplingDeformationConfiguration.Safe(config.grabStrength,260,0,20000);
            float grabRadius=DumplingDeformationConfiguration.Safe(config.grabRadius,2.8f,.1f,24);
            float pressRadius=DumplingDeformationConfiguration.Safe(config.pressRadius,1.65f,.1f,24);
            float pressStrength=DumplingDeformationConfiguration.Safe(config.pressStrength,.48f,0,8);
            float falloff=DumplingDeformationConfiguration.Safe(config.influenceFalloff,1.25f,.1f,5);
            float couplingStrength=DumplingDeformationConfiguration.Safe(config.neighborCoupling,45,0,10000);
            Vector2 inward=-contact.normalized; if(inward.sqrMagnitude<.01f) inward=Vector2.down;
            for(int i=0;i<Rest.Length;i++)
            {
                Vector3 displacement=Positions[i]-Rest[i];
                forces[i]=-displacement*stiffness*(Held?1:recovery);
                if(Held)
                {
                    float distance=Vector2.Distance(Rest[i],contact);
                    float w=Influence(distance,grabRadius,falloff);
                    float press=Influence(distance,pressRadius,falloff)*pressStrength;
                    Vector2 desired=(Vector2)grabRest[i]+resisted+inward*press;
                    forces[i]+=((Vector3)desired-displacement)*grabStrength*w;
                }
            }
            for(int e=0;e<neighbors.Length;e+=2)
            {
                int a=neighbors[e],b=neighbors[e+1];
                Vector3 coupling=((Positions[b]-Rest[b])-(Positions[a]-Rest[a]))*couplingStrength;
                forces[a]+=coupling/degree[a]; forces[b]-=coupling/degree[b];
            }
            float damping=Mathf.Exp(-DumplingDeformationConfiguration.Safe(config.damping,10,0,100)*dt);
            float velocityLimit=DumplingDeformationConfiguration.Safe(config.velocityLimit,24,1,120);
            for(int i=0;i<Rest.Length;i++)
            {
                Velocities[i]=Vector3.ClampMagnitude((Velocities[i]+forces[i]*dt)*damping,velocityLimit);
                offsets[i]=Vector3.ClampMagnitude(Positions[i]+Velocities[i]*dt-Rest[i],max);
            }
            // Bound each triangle's displacement Jacobian Frobenius norm. ||Du|| < 1
            // implies positive det(I+Du), bounded distortion and a globally injective PL map.
            float gradient=0;
            for(int t=0;t<Triangles.Length;t+=3)
            {
                int a=Triangles[t],b=Triangles[t+1],c=Triangles[t+2];
                Vector2 x=Rest[b]-Rest[a],y=Rest[c]-Rest[a],u=offsets[b]-offsets[a],v=offsets[c]-offsets[a];
                float determinant=Cross(x,y); if(Mathf.Abs(determinant)<.000001f) continue;
                Vector2 dx=(u*y.y-v*x.y)/determinant,dy=(-u*y.x+v*x.x)/determinant;
                gradient=Mathf.Max(gradient,Mathf.Sqrt(dx.sqrMagnitude+dy.sqrMagnitude));
            }
            float limit=config.SafeMaxStrain, factor=gradient>limit ? limit/gradient : 1;
            Strain=Mathf.Min(gradient,limit); MaximumDisplacement=MaximumSpeed=0;
            for(int i=0;i<Rest.Length;i++)
            {
                Vector3 next=Rest[i]+offsets[i]*factor;
                // Constraint correction removes energy; no constraint-induced velocity impulse.
                Velocities[i]*=factor; Positions[i]=next;
                MaximumDisplacement=Mathf.Max(MaximumDisplacement,(next-Rest[i]).magnitude);
                MaximumSpeed=Mathf.Max(MaximumSpeed,Velocities[i].magnitude);
            }
            float settle=DumplingDeformationConfiguration.Safe(config.settleThreshold,.002f,.0001f,.1f);
            if(!Held && MaximumDisplacement<settle && MaximumSpeed<settle) Reset();
        }
        public static float Influence(float distance,float radius,float falloff)
        { float x=Mathf.Clamp01(1-distance/Mathf.Max(.01f,radius)); return Mathf.Pow(x*x*(3-2*x),Mathf.Max(.1f,falloff)); }
        public static float Cross(Vector2 a,Vector2 b)=>a.x*b.y-a.y*b.x;
        public static bool Finite(Vector2 v)=>!float.IsNaN(v.x)&&!float.IsNaN(v.y)&&!float.IsInfinity(v.x)&&!float.IsInfinity(v.y);
    }
}
