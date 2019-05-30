/*************************************************************
 *
 *  MathJax/fonts/HTML-CSS/TeX/png/Main/Bold/CombDiactForSymbols.js
 *  
 *  Defines the image size data needed for the HTML-CSS OutputJax
 *  to display mathematics using fallback images when the fonts
 *  are not available to the client browser.
 *
 *  ---------------------------------------------------------------------
 *
 *  Copyright (c) 2009-2013 The MathJax Consortium
 *
 *  Licensed under the Apache License, Version 2.0 (the "License");
 *  you may not use this file except in compliance with the License.
 *  You may obtain a copy of the
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 *  Unless required by applicable law or agreed to in writing, software
 *  distributed under the License is distributed on an "AS IS" BASIS,
 *  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *  See the License for the specific language governing permissions and
 *  limitations under the License.
 *
 */

MathJax.OutputJax["HTML-CSS"].defineImageData({
  "MathJax_Main-bold": {
    0x20D7: [  // COMBINING RIGHT ARROW ABOVE
      [4,2,-3],[5,3,-4],[6,3,-5],[7,3,-6],[8,3,-7],[9,5,-8],[11,5,-10],[13,6,-11],
      [15,6,-14],[17,8,-16],[21,9,-20],[25,11,-23],[29,13,-28],[34,15,-33]
    ]
  }
});

MathJax.Ajax.loadComplete(MathJax.OutputJax["HTML-CSS"].imgDir+"/Main/Bold"+
                          MathJax.OutputJax["HTML-CSS"].imgPacked+"/CombDiactForSymbols.js");
