import{j as e}from"./jsx-runtime-CgLq-oUW.js";import{C as n,a as p,b as x,d as f,c as C,e as j}from"./card-C6gNvGae.js";import{c as A,S as $,a as M,B as N}from"./button-R1TzV71x.js";import{r as h}from"./index-2peij01d.js";import"./index-NmXEX80k.js";const m=h.forwardRef(({className:a,type:s,...r},t)=>e.jsx("input",{type:s,className:A("flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50",a),ref:t,...r}));m.displayName="Input";m.__docgenInfo={description:"",methods:[],displayName:"Input"};var k=["a","button","div","form","h2","h3","img","input","label","li","nav","ol","p","span","svg","ul"],O=k.reduce((a,s)=>{const r=h.forwardRef((t,i)=>{const{asChild:R,...I}=t,_=R?$:s;return typeof window<"u"&&(window[Symbol.for("radix-ui")]=!0),e.jsx(_,{...I,ref:i})});return r.displayName=`Primitive.${s}`,{...a,[s]:r}},{}),W="Label",H=h.forwardRef((a,s)=>e.jsx(O.label,{...a,ref:s,onMouseDown:r=>{var i;r.target.closest("button, input, select, textarea")||((i=a.onMouseDown)==null||i.call(a,r),!r.defaultPrevented&&r.detail>1&&r.preventDefault())}}));H.displayName=W;var L=H;const Y=M("text-sm font-medium leading-none peer-disabled:cursor-not-allowed peer-disabled:opacity-70"),u=h.forwardRef(({className:a,...s},r)=>e.jsx(L,{ref:r,className:A(Y(),a),...s}));u.displayName=L.displayName;u.__docgenInfo={description:"",methods:[]};const K={title:"Components/Card",component:n,parameters:{layout:"centered"},tags:["autodocs"]},o={render:()=>e.jsxs(n,{className:"w-[350px]",children:[e.jsxs(p,{children:[e.jsx(x,{children:"Card Title"}),e.jsx(f,{children:"Card description goes here"})]}),e.jsx(C,{children:e.jsx("p",{children:"Card content. Lorem ipsum dolor sit amet, consectetur adipiscing elit."})}),e.jsx(j,{children:e.jsx(N,{children:"Action"})})]})},d={render:()=>e.jsxs(n,{className:"w-[350px]",children:[e.jsxs(p,{children:[e.jsx(x,{children:"Create Account"}),e.jsx(f,{children:"Enter your details to create a new account"})]}),e.jsxs(C,{className:"space-y-4",children:[e.jsxs("div",{className:"space-y-2",children:[e.jsx(u,{htmlFor:"name",children:"Name"}),e.jsx(m,{id:"name",placeholder:"Enter your name"})]}),e.jsxs("div",{className:"space-y-2",children:[e.jsx(u,{htmlFor:"email",children:"Email"}),e.jsx(m,{id:"email",type:"email",placeholder:"Enter your email"})]})]}),e.jsx(j,{children:e.jsx(N,{className:"w-full",children:"Create Account"})})]})},l={render:()=>e.jsxs(n,{className:"w-[350px]",children:[e.jsxs(p,{children:[e.jsx(x,{children:"Basic Plan"}),e.jsx(f,{children:"Perfect for getting started"})]}),e.jsxs(C,{children:[e.jsx("div",{className:"text-3xl font-bold",children:"$9.99/mo"}),e.jsxs("ul",{className:"mt-4 space-y-2 text-sm",children:[e.jsx("li",{children:"✓ 100 messages per month"}),e.jsx("li",{children:"✓ Basic support"}),e.jsx("li",{children:"✓ Email notifications"})]})]}),e.jsx(j,{children:e.jsx(N,{variant:"outline",className:"w-full",children:"Choose Plan"})})]})},c={render:()=>e.jsxs(n,{className:"w-[350px]",children:[e.jsxs(p,{children:[e.jsx(x,{children:"Statistics"}),e.jsx(f,{children:"Your account metrics"})]}),e.jsx(C,{children:e.jsxs("div",{className:"space-y-2",children:[e.jsxs("div",{className:"flex justify-between",children:[e.jsx("span",{className:"text-muted-foreground",children:"Total Plans"}),e.jsx("span",{className:"font-semibold",children:"12"})]}),e.jsxs("div",{className:"flex justify-between",children:[e.jsx("span",{className:"text-muted-foreground",children:"Active Subscribers"}),e.jsx("span",{className:"font-semibold",children:"1,234"})]}),e.jsxs("div",{className:"flex justify-between",children:[e.jsx("span",{className:"text-muted-foreground",children:"Revenue"}),e.jsx("span",{className:"font-semibold",children:"$12,345"})]})]})})]})};var b,v,g;o.parameters={...o.parameters,docs:{...(b=o.parameters)==null?void 0:b.docs,source:{originalSource:`{
  render: () => <Card className="w-[350px]">\r
      <CardHeader>\r
        <CardTitle>Card Title</CardTitle>\r
        <CardDescription>Card description goes here</CardDescription>\r
      </CardHeader>\r
      <CardContent>\r
        <p>Card content. Lorem ipsum dolor sit amet, consectetur adipiscing elit.</p>\r
      </CardContent>\r
      <CardFooter>\r
        <Button>Action</Button>\r
      </CardFooter>\r
    </Card>
}`,...(g=(v=o.parameters)==null?void 0:v.docs)==null?void 0:g.source}}};var y,w,E;d.parameters={...d.parameters,docs:{...(y=d.parameters)==null?void 0:y.docs,source:{originalSource:`{
  render: () => <Card className="w-[350px]">\r
      <CardHeader>\r
        <CardTitle>Create Account</CardTitle>\r
        <CardDescription>Enter your details to create a new account</CardDescription>\r
      </CardHeader>\r
      <CardContent className="space-y-4">\r
        <div className="space-y-2">\r
          <Label htmlFor="name">Name</Label>\r
          <Input id="name" placeholder="Enter your name" />\r
        </div>\r
        <div className="space-y-2">\r
          <Label htmlFor="email">Email</Label>\r
          <Input id="email" type="email" placeholder="Enter your email" />\r
        </div>\r
      </CardContent>\r
      <CardFooter>\r
        <Button className="w-full">Create Account</Button>\r
      </CardFooter>\r
    </Card>
}`,...(E=(w=d.parameters)==null?void 0:w.docs)==null?void 0:E.source}}};var F,B,P;l.parameters={...l.parameters,docs:{...(F=l.parameters)==null?void 0:F.docs,source:{originalSource:`{
  render: () => <Card className="w-[350px]">\r
      <CardHeader>\r
        <CardTitle>Basic Plan</CardTitle>\r
        <CardDescription>Perfect for getting started</CardDescription>\r
      </CardHeader>\r
      <CardContent>\r
        <div className="text-3xl font-bold">$9.99/mo</div>\r
        <ul className="mt-4 space-y-2 text-sm">\r
          <li>✓ 100 messages per month</li>\r
          <li>✓ Basic support</li>\r
          <li>✓ Email notifications</li>\r
        </ul>\r
      </CardContent>\r
      <CardFooter>\r
        <Button variant="outline" className="w-full">\r
          Choose Plan\r
        </Button>\r
      </CardFooter>\r
    </Card>
}`,...(P=(B=l.parameters)==null?void 0:B.docs)==null?void 0:P.source}}};var D,T,S;c.parameters={...c.parameters,docs:{...(D=c.parameters)==null?void 0:D.docs,source:{originalSource:`{
  render: () => <Card className="w-[350px]">\r
      <CardHeader>\r
        <CardTitle>Statistics</CardTitle>\r
        <CardDescription>Your account metrics</CardDescription>\r
      </CardHeader>\r
      <CardContent>\r
        <div className="space-y-2">\r
          <div className="flex justify-between">\r
            <span className="text-muted-foreground">Total Plans</span>\r
            <span className="font-semibold">12</span>\r
          </div>\r
          <div className="flex justify-between">\r
            <span className="text-muted-foreground">Active Subscribers</span>\r
            <span className="font-semibold">1,234</span>\r
          </div>\r
          <div className="flex justify-between">\r
            <span className="text-muted-foreground">Revenue</span>\r
            <span className="font-semibold">$12,345</span>\r
          </div>\r
        </div>\r
      </CardContent>\r
    </Card>
}`,...(S=(T=c.parameters)==null?void 0:T.docs)==null?void 0:S.source}}};const Q=["Basic","WithForm","PlanCard","NoFooter"];export{o as Basic,c as NoFooter,l as PlanCard,d as WithForm,Q as __namedExportsOrder,K as default};
