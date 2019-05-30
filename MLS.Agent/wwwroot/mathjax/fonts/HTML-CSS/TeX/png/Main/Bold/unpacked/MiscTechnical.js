/*************************************************************
 *
 *  MathJax/fonts/HTML-CSS/TeX/png/Main/Bold/MiscTechnical.js
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
    0x2308: [  // LEFT CEILING
      [4,8,2],[4,9,3],[5,11,3],[6,12,3],[8,15,4],[9,18,5],[10,20,5],[12,24,6],
      [14,28,7],[16,33,8],[20,40,10],[23,47,12],[28,56,14],[33,67,17]
    ],
    0x2309: [  // RIGHT CEILING
      [3,7,2],[3,9,3],[3,11,3],[4,12,3],[5,15,4],[6,18,5],[7,20,5],[8,24,6],
      [9,28,7],[11,33,8],[13,40,10],[15,47,12],[18,56,14],[21,67,17]
    ],
    0x230A: [  // LEFT FLOOR
      [4,8,2],[4,9,2],[5,11,3],[6,12,3],[8,15,4],[9,18,5],[10,20,5],[12,24,6],
      [14,28,7],[16,34,9],[20,40,10],[23,47,12],[28,56,14],[33,67,17]
    ],
    0x230B: [  // RIGHT FLOOR
      [3,8,2],[3,9,2],[3,11,3],[4,12,3],[5,15,4],[6,18,5],[7,20,5],[8,24,6],
      [9,28,7],[11,34,9],[13,40,10],[15,47,12],[18,56,14],[21,67,17]
    ],
    0x2322: [  // stix-small down curve
      [8,3,0],[9,4,0],[11,3,-1],[13,4,-1],[15,5,-1],[18,6,-1],[22,6,-2],[26,8,-2],
      [30,10,-2],[36,11,-3],[43,12,-4],[51,14,-5],[60,18,-5],[72,20,-7]
    ],
    0x2323: [  // stix-small up curve
      [8,2,-1],[10,3,-1],[11,3,-1],[13,4,-1],[16,5,-1],[19,5,-2],[22,6,-2],[26,8,-2],
      [31,8,-3],[36,9,-4],[43,12,-4],[51,14,-5],[61,16,-6],[72,18,-8]
    ]
  }
});

MathJax.Ajax.loadComplete(MathJax.OutputJax["HTML-CSS"].imgDir+"/Main/Bold"+
                          MathJax.OutputJax["HTML-CSS"].imgPacked+"/MiscTechnical.js");
