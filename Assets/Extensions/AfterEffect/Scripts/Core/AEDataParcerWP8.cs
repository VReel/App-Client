////////////////////////////////////////////////////////////////////////////////
using System;

#if !UNITY_WEBPLAYER
using System.Xml.Linq;
#endif
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class AEDataParcerWP8  {
	
	private static float frameDuration;
	
	public static AEAnimationData ParceAnimationData(string data) {

		#if !UNITY_WEBPLAYER

		XDocument xmlDoc = XDocument.Load(new StringReader(data));
		
		AEAnimationData animation = new AEAnimationData ();

		XElement meta_element 	=  xmlDoc.Root.Element ("meta");//xmlDoc.Root == after_affect_animation_doc
		animation.frameDuration =  GetFloat(meta_element.Attribute("frameDuration").Value);
		animation.totalFrames 	=  GetInt(meta_element.Attribute("totalFrames").Value);
		animation.duration	 	=  GetFloat(meta_element.Attribute("duration").Value);
		
		frameDuration = animation.frameDuration;

		foreach (XElement element in xmlDoc.Root.Elements()) {//xmlDoc.Root == after_affect_animation_doc		
			switch(element.Name.ToString()) {
				case "composition":
					animation.composition = ParseComposition (element);
					break;
				case "sub_items":
					foreach (XElement sub_element in element.Elements()) {
						animation.addComposition(ParseComposition (sub_element));
					}
					break;
			}
		}

		return animation;
		#else
		return null;
		#endif
	}
	
	#if !UNITY_WEBPLAYER
	public static AECompositionTemplate ParseComposition(XElement composition) {


		AECompositionTemplate comp = new AECompositionTemplate ();
		
		comp.id    = GetInt(composition.Attribute("id").Value);
		comp.width = GetFloat(composition.Attribute("w").Value);
		comp.heigh = GetFloat(composition.Attribute("h").Value);

		XElement meta_element = composition.Element("meta");
		comp.duration = GetFloat (meta_element.Attribute ("duration").Value);
		comp.totalFrames = GetInt (meta_element.Attribute ("totalFrames").Value);
		comp.frameDuration = frameDuration;


		foreach (XElement element in composition.Elements("layer")) {
				AELayerTemplate layer = new AELayerTemplate ();
				
				string layerType = element.Attribute("type").Value;
				
				if(layerType.Equals("Composition")) {
					layer.type = AELayerType.COMPOSITION;
					layer.id  = GetInt(element.Attribute("id").Value);	
				}
				
				if(layerType.Equals("Footage")) {
					layer.type = AELayerType.FOOTAGE;
					layer.source = element.Attribute("source").Value;
				}
				
				
				layer.index = Int32.Parse (element.Attribute("index").Value);
				if(element.Attribute("parent").Value != "none") {
					layer.parent = Int32.Parse(element.Attribute("parent").Value);
				}  else {
					layer.parent = 0;
				}
				
				layer.width  = GetInt(element.Attribute("w").Value);
				layer.height = GetInt(element.Attribute("h").Value);
				layer.name = element.Attribute("name").Value;


				XAttribute sourceType = element.Attribute("sourceType");				
				if(sourceType != null) {
					layer.footageType = (AEFootageType) Enum.Parse(typeof(AEFootageType), sourceType.Value);
				}
				
				string blendMode = element.Attribute("blending").Value;
				layer.blending = (AELayerBlendingType) System.Enum.Parse (typeof(AELayerBlendingType), blendMode);

				float inTime  = GetFloat (element.Attribute("inPoint").Value);
				float outTime = GetFloat (element.Attribute("outPoint").Value);
				layer.setInOutTime (inTime, outTime, comp);

				
				
				AEFrameTemplate lastFrame = null;
				foreach (XElement sub_element in element.Elements()) {//keyframe
					AEFrameTemplate frame = new AEFrameTemplate ();
					
					frame.index = Int32.Parse(sub_element.Attribute("frame").Value);
										
					foreach (XElement attribute in sub_element.Elements()) {//source
												
						foreach (XElement sub_attribute in attribute.Elements()) {//property
							string propType = sub_attribute.Attribute ("name").Value;

							switch(propType) {
								case AEPropertyType.ANCHOR_POINT:									
									frame.pivot = new Vector3 (GetFloat(sub_attribute.Attribute ("x").Value), GetFloat(sub_attribute.Attribute ("y").Value), GetFloat (sub_attribute.Attribute ("z").Value));
									break;
								case AEPropertyType.POSITION:
									frame.SetPosition(new Vector3 (GetFloat(sub_attribute.Attribute("x").Value),GetFloat(sub_attribute.Attribute("y").Value),GetFloat(sub_attribute.Attribute("z").Value)));
									break;
								case AEPropertyType.SCALE:
									frame.scale = new Vector3 (GetFloat(sub_attribute.Attribute ("x").Value), GetFloat(sub_attribute.Attribute ("y").Value), GetFloat(sub_attribute.Attribute ("z").Value)) * 0.01f;
									break;
								case AEPropertyType.ROTATION:
									frame.rotation = GetFloat(sub_attribute.Attribute ("val").Value);
									break;
								case AEPropertyType.OPACITY:
									frame.opacity = GetFloat(sub_attribute.Attribute ("val").Value);
									break;
							}
						}
						
						frame.CompareToFrame (lastFrame);
						
						lastFrame = frame;
						layer.addFrame (frame);
					}
					
				}
				
				comp.addLayer (layer);
		}

		return comp;



	}
	#endif
	
	public static float GetFloat(string name) {
		return System.Convert.ToSingle(name);
	}
	
	public static int GetInt(string name) {
		return System.Convert.ToInt32(name);
	}
}

