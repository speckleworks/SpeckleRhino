import Vue from 'vue'
import App from './App.vue'
import VueMaterial from 'vue-material'

import Store from './store/store'

Vue.use( VueMaterial )

Vue.material.registerTheme('default', {
  primary: 'black',
  accent: 'light-blue',
  warn: 'red',
  background: 'white'
})

new Vue( {
  el: '#app',
  store: Store,
  render: h => h( App )
} )