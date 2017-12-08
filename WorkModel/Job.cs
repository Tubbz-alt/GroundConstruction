﻿//   Job.cs
//
//  Author:
//       Allis Tauri <allista@gmail.com>
//
//  Copyright (c) 2017 Allis Tauri
using System;
using AT_Utils;

namespace GroundConstruction
{
    /// <summary>
    /// Job is a sequence of tasks of different types.
    /// It also contains a set of parameters that change in the course of the work being done.
    /// </summary>
    public partial class Job : Work
    {
        public class Param : ConfigNodeObject
        {
            [Persistent] public float Value;
            [Persistent] public FloatCurve Curve = new FloatCurve();

            public float Min { get { return Curve.Curve.keys[0].value; } }
            public float Max { get { return Curve.Curve.keys[Curve.Curve.length-1].value; } }

            public Param()
            {
                Curve.Add(0, 0);
            }

            public void Update(float fraction)
            {
                Value = Curve.Evaluate(fraction);
            }
        }

        [Persistent] public string Name = "";
        [Persistent] public PersistentDictS<Param> Parameters = new PersistentDictS<Param>();

        public Task First { get; protected set; }
        public Task Last { get; protected set; }
        public Task Current { get; protected set; }

        public virtual bool Valid { get { return First != null; } }
        public override bool Complete { get { return First != null && Current == null; } }

        public double Fraction()
        { 
            return Current != null? 
                Current.WorkDoneWithPrev/TotalWork : 
                (First != null? 1.0 : 0.0); 
        }

        public Job CurrentSubJob
        {
            get 
            {
                return Current == null? 
                    this : Current.CurrentSubtask.Job;
            }
        }

        public void AddSubJob(Job sub)
        {
            if(First != null)
                First.AddSubtask(sub.First);
        }

        public double DoSomeWork(double work)
        {
            return Current != null? 
                Current.DoSomeWork(work) : work;
        }

        public void UpdateCurrentTask()
        {
            Current = First;
            while(Current != null && Current.Complete)
                Current = Current.Next;
        }

        public override void SetComplete(bool complete)
        {
            if(First != null)
            {
                Current = null;
                if(complete) Last.SetComplete(complete);
                else First.SetComplete(complete);
            }
        }

        public override void Load(ConfigNode node)
        {
            base.Load(node);
            UpdateCurrentTask();
        }

        protected void update_total_work()
        {
            if(Last != null)
                TotalWork = Last.TotalWorkWithPrev;
        }

        void update_params()
        {
            if(Current != null)
            {
                var frac = Current.Fraction;
                foreach(var val in Parameters.Values)
                    val.Update((float)frac);
            }
        }

        protected Task add_task(ResourceUsageInfo resource, float end_fraction)
        {
            return new Task(this, resource, end_fraction);
        }

        public static implicit operator bool(Job job) { return job != null && job.Valid; }
    }
}

