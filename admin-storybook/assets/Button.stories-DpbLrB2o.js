import{j as e}from"./jsx-runtime-CgLq-oUW.js";import{B as r}from"./button-R1TzV71x.js";import{r as i}from"./index-2peij01d.js";/**
 * @license lucide-react v0.453.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const Z=t=>t.replace(/([a-z0-9])([A-Z])/g,"$1-$2").toLowerCase(),W=(...t)=>t.filter((a,n,s)=>!!a&&s.indexOf(a)===n).join(" ");/**
 * @license lucide-react v0.453.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */var J={xmlns:"http://www.w3.org/2000/svg",width:24,height:24,viewBox:"0 0 24 24",fill:"none",stroke:"currentColor",strokeWidth:2,strokeLinecap:"round",strokeLinejoin:"round"};/**
 * @license lucide-react v0.453.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const Q=i.forwardRef(({color:t="currentColor",size:a=24,strokeWidth:n=2,absoluteStrokeWidth:s,className:g="",children:o,iconNode:H,..._},q)=>i.createElement("svg",{ref:q,...J,width:a,height:a,stroke:t,strokeWidth:s?Number(n)*24/Number(a):n,className:W("lucide",g),..._},[...H.map(([F,K])=>i.createElement(F,K)),...Array.isArray(o)?o:[o]]));/**
 * @license lucide-react v0.453.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const x=(t,a)=>{const n=i.forwardRef(({className:s,...g},o)=>i.createElement(Q,{ref:o,iconNode:a,className:W(`lucide-${Z(t)}`,s),...g}));return n.displayName=`${t}`,n};/**
 * @license lucide-react v0.453.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const U=x("Download",[["path",{d:"M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4",key:"ih7n3h"}],["polyline",{points:"7 10 12 15 17 10",key:"2ggqvy"}],["line",{x1:"12",x2:"12",y1:"15",y2:"3",key:"1vk2je"}]]);/**
 * @license lucide-react v0.453.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const X=x("Plus",[["path",{d:"M5 12h14",key:"1ays0h"}],["path",{d:"M12 5v14",key:"s699le"}]]);/**
 * @license lucide-react v0.453.0 - ISC
 *
 * This source code is licensed under the ISC license.
 * See the LICENSE file in the root directory of this source tree.
 */const Y=x("Trash2",[["path",{d:"M3 6h18",key:"d0wm0j"}],["path",{d:"M19 6v14c0 1-1 2-2 2H7c-1 0-2-1-2-2V6",key:"4alrt4"}],["path",{d:"M8 6V4c0-1 1-2 2-2h4c1 0 2 1 2 2v2",key:"v07s0e"}],["line",{x1:"10",x2:"10",y1:"11",y2:"17",key:"1uufr5"}],["line",{x1:"14",x2:"14",y1:"11",y2:"17",key:"xtxkd"}]]),ae={title:"Components/Button",component:r,parameters:{layout:"centered"},tags:["autodocs"],argTypes:{variant:{control:"select",options:["default","secondary","outline","ghost","link","destructive"]},size:{control:"select",options:["default","sm","lg","icon"]}}},c={args:{children:"Button",variant:"default"}},l={args:{children:"Secondary",variant:"secondary"}},d={args:{children:"Outline",variant:"outline"}},u={args:{children:"Ghost",variant:"ghost"}},m={args:{children:"Delete",variant:"destructive"}},h={args:{children:e.jsxs(e.Fragment,{children:[e.jsx(X,{className:"mr-2 h-4 w-4"}),"Add Item"]})}},p={args:{size:"icon",children:e.jsx(Y,{className:"h-4 w-4"}),variant:"destructive"}},v={render:()=>e.jsxs("div",{className:"flex flex-col gap-4",children:[e.jsxs("div",{className:"flex gap-2",children:[e.jsx(r,{variant:"default",children:"Default"}),e.jsx(r,{variant:"secondary",children:"Secondary"}),e.jsx(r,{variant:"outline",children:"Outline"}),e.jsx(r,{variant:"ghost",children:"Ghost"}),e.jsx(r,{variant:"link",children:"Link"}),e.jsx(r,{variant:"destructive",children:"Destructive"})]}),e.jsxs("div",{className:"flex gap-2 items-center",children:[e.jsx(r,{size:"sm",children:"Small"}),e.jsx(r,{size:"default",children:"Default"}),e.jsx(r,{size:"lg",children:"Large"}),e.jsx(r,{size:"icon",children:e.jsx(U,{className:"h-4 w-4"})})]}),e.jsxs("div",{className:"flex gap-2",children:[e.jsx(r,{disabled:!0,children:"Disabled"}),e.jsx(r,{variant:"outline",disabled:!0,children:"Disabled Outline"})]})]})};var f,y,B;c.parameters={...c.parameters,docs:{...(f=c.parameters)==null?void 0:f.docs,source:{originalSource:`{
  args: {
    children: "Button",
    variant: "default"
  }
}`,...(B=(y=c.parameters)==null?void 0:y.docs)==null?void 0:B.source}}};var j,k,w;l.parameters={...l.parameters,docs:{...(j=l.parameters)==null?void 0:j.docs,source:{originalSource:`{
  args: {
    children: "Secondary",
    variant: "secondary"
  }
}`,...(w=(k=l.parameters)==null?void 0:k.docs)==null?void 0:w.source}}};var D,N,S;d.parameters={...d.parameters,docs:{...(D=d.parameters)==null?void 0:D.docs,source:{originalSource:`{
  args: {
    children: "Outline",
    variant: "outline"
  }
}`,...(S=(N=d.parameters)==null?void 0:N.docs)==null?void 0:S.source}}};var b,O,z;u.parameters={...u.parameters,docs:{...(b=u.parameters)==null?void 0:b.docs,source:{originalSource:`{
  args: {
    children: "Ghost",
    variant: "ghost"
  }
}`,...(z=(O=u.parameters)==null?void 0:O.docs)==null?void 0:z.source}}};var A,I,L;m.parameters={...m.parameters,docs:{...(A=m.parameters)==null?void 0:A.docs,source:{originalSource:`{
  args: {
    children: "Delete",
    variant: "destructive"
  }
}`,...(L=(I=m.parameters)==null?void 0:I.docs)==null?void 0:L.source}}};var C,E,G;h.parameters={...h.parameters,docs:{...(C=h.parameters)==null?void 0:C.docs,source:{originalSource:`{
  args: {
    children: <>\r
        <Plus className="mr-2 h-4 w-4" />\r
        Add Item\r
      </>
  }
}`,...(G=(E=h.parameters)==null?void 0:E.docs)==null?void 0:G.source}}};var M,T,V;p.parameters={...p.parameters,docs:{...(M=p.parameters)==null?void 0:M.docs,source:{originalSource:`{
  args: {
    size: "icon",
    children: <Trash2 className="h-4 w-4" />,
    variant: "destructive"
  }
}`,...(V=(T=p.parameters)==null?void 0:T.docs)==null?void 0:V.source}}};var $,P,R;v.parameters={...v.parameters,docs:{...($=v.parameters)==null?void 0:$.docs,source:{originalSource:`{
  render: () => <div className="flex flex-col gap-4">\r
      <div className="flex gap-2">\r
        <Button variant="default">Default</Button>\r
        <Button variant="secondary">Secondary</Button>\r
        <Button variant="outline">Outline</Button>\r
        <Button variant="ghost">Ghost</Button>\r
        <Button variant="link">Link</Button>\r
        <Button variant="destructive">Destructive</Button>\r
      </div>\r
      <div className="flex gap-2 items-center">\r
        <Button size="sm">Small</Button>\r
        <Button size="default">Default</Button>\r
        <Button size="lg">Large</Button>\r
        <Button size="icon">\r
          <Download className="h-4 w-4" />\r
        </Button>\r
      </div>\r
      <div className="flex gap-2">\r
        <Button disabled>Disabled</Button>\r
        <Button variant="outline" disabled>\r
          Disabled Outline\r
        </Button>\r
      </div>\r
    </div>
}`,...(R=(P=v.parameters)==null?void 0:P.docs)==null?void 0:R.source}}};const ne=["Default","Secondary","Outline","Ghost","Destructive","WithIcon","IconOnly","AllVariants"];export{v as AllVariants,c as Default,m as Destructive,u as Ghost,p as IconOnly,d as Outline,l as Secondary,h as WithIcon,ne as __namedExportsOrder,ae as default};
