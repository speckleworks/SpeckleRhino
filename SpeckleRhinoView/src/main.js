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

    EventBus.$on( 'client-purge', () => {
      console.log( 'purge-purge' )
      this.$store.commit( 'PURGE_CLIENTS' )
    })

    EventBus.$on( 'client-add', ( streamId, data ) => {
      console.log( 'client-add', streamId, JSON.parse( data ) ) 
      this.$store.commit( 'ADD_CLIENT', JSON.parse( data ) )
    })

    EventBus.$on( 'client-metadata-update', ( streamId, data ) => {
      console.log( 'client-metadata-update', streamId, data )
      this.$store.commit( 'SET_METADATA', { streamId: streamId, stream: JSON.parse( data ) } )
    })

    EventBus.$on( 'client-log', ( streamId, data ) => {
      console.log( data )
      console.log( 'client-log', streamId, JSON.parse( data ) )
      this.$store.commit( 'APPEND_LOG', { streamId: streamId, data: JSON.parse( data ) } )
    })

    EventBus.$on( 'client-is-loading', ( streamId, data ) => {
      console.log( 'client-is-loading', streamId, data )
      this.$store.commit( 'SET_LOADING', { streamId: streamId, status: true } )
    })

    EventBus.$on( 'client-done-loading', ( streamId, data ) => {
      console.log( 'client-done-loading', streamId, data )
      this.$store.commit( 'SET_LOADING', { streamId: streamId, status: false } )
    })

    EventBus.$on( 'client-expired', ( streamId, data ) => {
      console.log( 'client-expired', streamId, data )
      this.$store.commit( 'SET_EXPIRED', { streamId: streamId, status: true } )
    })

    EventBus.$on( 'client-ws-message', ( streamId, data ) => {
      console.log( 'client-ws-message', streamId, JSON.parse( data ) )
    })

    // tell .net that the app is ± ready.
    Interop.appReady()
  }
} )

window.EventBus = EventBus // expose globally for .net
window.Store = Store // expose globally for .net? 