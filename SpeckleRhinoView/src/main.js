import Vue from 'vue'
import Vuex from 'vuex'
import Vuetify from 'vuetify'
import VueTimeago from 'vue-timeago'

import App from './App.vue'
import Store from './store/store.js'

import { EventBus } from './event-bus'

Vue.use( Vuex )
Vue.use( Vuetify )

Vue.use( VueTimeago, {
  name: 'timeago',
  locale: 'en-US',
  locales: {
    'en-US': require('vue-timeago/locales/en-US.json')
  }
})

new Vue( {
  store: Store,
  el: '#app',
  render: h => h( App ),
  mounted() {
    // Populate with existing accounts
    this.$store.dispatch( 'getUserAccounts' )
    // Populate with existing clients
    this.$store.dispatch( 'getFileStreams' )

    EventBus.$on( 'client-add', ( streamId, data ) => {
      console.log( 'add', JSON.parse( data ) )
      console.log( 'client-add', streamId, JSON.parse( data ) ) 
      this.$store.commit( 'ADD_CLIENT', JSON.parse( data ) )
    })

    EventBus.$on( 'client-log', ( streamId, data ) => {
      console.log( 'client-log', streamId, JSON.parse( data ) )
      this.$store.commit( 'APPEND_LOG', { streamId: streamId, data: JSON.parse( data ) } )
    })

    EventBus.$on( 'client-ws-message', ( streamId, data ) => {
      console.log( 'client-ws-message', streamId, JSON.parse( data ) )
    })

  }
} )

window.EventBus = EventBus // expose globally for .net