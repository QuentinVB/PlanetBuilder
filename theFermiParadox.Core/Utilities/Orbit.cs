﻿using Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using theFermiParadox.Core.Abstracts;
using theFermiParadox.Core.Utilities;

namespace theFermiParadox.Core
{
//https://en.wikipedia.org/wiki/Kepler%27s_laws_of_planetary_motion#Position_as_a_function_of_time
//https://space.stackexchange.com/questions/8911/determining-orbital-position-at-a-future-point-in-time
    public class Orbit : ICloneable
    {
        private double _eccentricity;
        private Angle _meanAnomaly;

        private double _apoapsis;
        private double _periapsis;
        
        private Angle _inclination=0;
        private Angle _longitudeOfAscendingNode = 0;
        private Angle _argumentOfPeriapsis = 0;

        private readonly DateTime _startDate;
        private TimeSpan _orbitalPeriod;

        private TimeSpan _time;
        private double _eccentricAnomaly=0;


        //private Vector3 _position;
        IOrbitable _mainBody;
        IOrbitable _body;

        /// <summary>
        /// Define the orbit between 2 body. The most massive will be the main body.
        /// </summary>
        /// <param name="bodyA">The first body of the orbit</param>
        /// <param name="bodyB">The second body of the orbit</param>
        /// <param name="eccentricity">derivation from perfect circle : 0 is circle, below 1 is elipsoid, above is a escape trajectory (no units)</param>
        /// <param name="meanDistance">aka the semi Major axis of the orbit (in meters)</param>
        public Orbit(IOrbitable bodyA, IOrbitable bodyB, DateTime epoch, double eccentricity, double meanDistance)
        {
            //Setup bodies position according to their weights

            if (bodyA.IsVirtual) { _mainBody = bodyA; _body = bodyB; }
            else if (bodyB.IsVirtual) { _mainBody = bodyB; _body = bodyA; }
            else if (bodyA is APhysicalObject bodyAphysical && bodyB is APhysicalObject bodyBphysical)
            {
                if (bodyAphysical.Mass >= bodyBphysical.Mass)
                {
                    _mainBody = bodyA; _body = bodyB;
                }
                else
                {
                    _mainBody = bodyB; _body = bodyA;
                }
            }
            else
            {
                _mainBody = bodyA; _body = bodyB;
            }

            //Bind the body to this orbit
            _mainBody.ChildOrbit.Add(this);
            _body.ParentOrbit = this;

            //initialise internal value
            _startDate = epoch;
            _time = new TimeSpan();
            _eccentricity = eccentricity;
            _meanAnomaly = 0;
            _apoapsis = meanDistance * (1 + eccentricity);
            _periapsis = meanDistance * (1 - eccentricity);

            //THAR BE DRAGONZ
            long nanosecondPeriod = (long)(
                    1e+9 * 2 * Math.PI * Math.Sqrt(
                            SemiMajorAxis * SemiMajorAxis * SemiMajorAxis /
                            (Physic.GravitationalConstant * _mainBody.Mass)
                            )
                     );
            _orbitalPeriod = new TimeSpan(nanosecondPeriod
                );
        }
        //INITIALS

        /// <summary>
        /// farthest point to its orbital focus (in meters)
        /// </summary>
        public double Apoapsis { get => _apoapsis; }
        /// <summary>
        /// nearest point to its orbital focus (in meters)
        /// </summary>
        public double Periapsis { get => _periapsis; }
        /// <summary>
        /// period aroud the orbital focus (in seconds)
        /// </summary>
        public TimeSpan OrbitalPeriod { get => _orbitalPeriod; }
        /// <summary>
        /// derivation from perfect circle : 0 is circle, below 1 is elipsoid, above is a escape trajectory (no units)
        /// </summary>
        public double Eccentricity { get => _eccentricity; }
        /// <summary>
        /// time since t0
        /// </summary>
        public TimeSpan ElapsedTime { get => _time; }
        /// <summary>
        /// The t0 of the orbit, when smaller body is at periapsis, used to calculate every position from this reference point in time
        /// </summary>
        public DateTime Epoch => _startDate;
        /// <summary>
        /// The actual time, in human earth format
        /// </summary>
        public DateTime Date => _startDate.AddSeconds(_time.TotalSeconds);
        /// <summary>
        /// And updated each time the time update
        /// </summary>
        public Angle MeanAnomaly { get => _meanAnomaly; }


        //DERIVED
        /// <summary>
        /// the major axis, the longest diameter (in meters)
        /// </summary>
        public double MajorAxis { get => _periapsis+_apoapsis; }
        /// <summary>
        /// the semi major axis, the longest diameter divided by 2(in meters)
        /// </summary>
        public double SemiMajorAxis { get => MajorAxis / 2; }
        /// <summary>
        /// the minor axis, the shortest diameter (in meters)
        /// </summary>
        public double MinorAxis { get => SemiMinorAxis*2; }
        /// <summary>
        /// the minor axis, the shortest diameter (in meters)
        /// </summary>
        public double SemiMinorAxis { get => Math.Sqrt(_apoapsis*_periapsis); }
        /// <summary>
        /// the chord above the focii (in meters)
        /// </summary>
        public double SemiLatusRectum { get => Math.Pow(SemiMajorAxis,2)/SemiMajorAxis; }
        /// <summary>
        /// mean motion around the orbit (in m.s-1)
        /// </summary>
        public double MeanMotion { get => Math.Sqrt(
                    (Physic.GravitationalConstant *(_mainBody.Mass + _body.Mass )) /Math.Pow(SemiMajorAxis,3)); }

        //VARIABLE IN TIME
        /// <summary>
        /// The current time of the system, thus derivate all the motion calculus
        /// </summary>
        public TimeSpan CurrentTime { get => _time; }

        /// <summary>
        /// The local eccentric anomaly, recalculated everytime the time update
        /// </summary>
        public double EccentricAnomaly { get => _eccentricAnomaly; }
        /// <summary>
        /// the current distance between the main body and the smaller body
        /// </summary>
        public double CurrentRadius { get => SemiMajorAxis * (1 - Math.Cos(EccentricAnomaly)); }
        /// <summary>
        /// angle between periapsis and actual position of the smaller body on the orbit (i in rad)
        /// </summary>
        public Angle TrueAnomaly { get => 2 * Math.Atan(Math.Sqrt((1+Eccentricity)/(1-Eccentricity)) * Math.Tan(EccentricAnomaly/2) ); }

        //INCLINAISON (NOT SUPPORTED)
        /// <summary>
        /// inclination of the orbit from ecliptic (i in degree)
        /// </summary>
        //public Angle Inclination { get => _inclination; }
        /// <summary>
        ///(in degree)
        /// </summary>
        //public Angle LongitudeOfAscendingNode { get => _longitudeOfAscendingNode; }
        /// <summary>
        /// (in degree)
        /// </summary>
        //public Angle ArgumentOfPeriapsis { get => _argumentOfPeriapsis; }


        //DRAWING
        public Vector3 CenterPosition { get { return new Vector3(); } }
        [NonPrintable]
        public IOrbitable MainBody { get => _mainBody; }
        [NonPrintable]
        public IOrbitable Body { get => _body; }

        public double Width { get => MajorAxis; }
        public double Height { get=> MinorAxis; }

        public Vector3 CurrentBodyPosition => new Vector3(
            SemiMajorAxis * (Math.Cos(EccentricAnomaly) - Eccentricity),
            SemiMinorAxis*Math.Sin(EccentricAnomaly),
            0
            );


        /* not so
         * LOTS OF MATHS HERE
         */
        public void UpdateTime(TimeSpan timeOffset)
        {
            _time += timeOffset;
            //THAR BE DRAGONZ
            //add mean anomaly at 0 
            _meanAnomaly =  (MeanMotion * _time.TotalSeconds)%(2*Math.PI); 
            _eccentricAnomaly = ComputeEccentricAnomaly(Eccentricity, MeanAnomaly);
        }

        private double ComputeEccentricAnomaly(double eccentricity,double meanAnomaly)
        {
            double E = meanAnomaly;
            double dE = 1;
            while (Math.Abs(dE) < 1e-6)
            {
                dE = (E - eccentricity * Math.Sin(E) - meanAnomaly) / (1 - eccentricity * Math.Cos(E));
                E -= dE;
            }
            return E;
         }
        public override string ToString()
        {
            string rslt = "";
            foreach (PropertyInfo p in this.GetType().GetProperties().ToList())
            {
                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(p);
                bool isPrintable=true;
                foreach (System.Attribute attr in attrs)
                {
                    if (attr is NonPrintable)
                    {
                        isPrintable = false;
                    }
                }
                if(isPrintable)
                {
                    rslt += "  " + p.Name + " : " + (this.GetType().GetProperty(p.Name).GetValue(this, null) ?? "none").ToString() + "\n";

                }
                else
                {
                    rslt += "  " + p.Name + "\n";
                }
            }
            return rslt;
        }

        public object Clone()
        {
             return this.MemberwiseClone(); ;        
        }
    }
}
