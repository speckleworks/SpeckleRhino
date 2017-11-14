import Vue from 'vue'
import Vuex from 'vuex'

Vue.use( Vuex )

export default new Vuex.Store( {
  state: {
    accounts: [ ],
    clients: [ ]
  },
  getters: {
    accounts: state => state.accounts,
    clients: state => state.clients
  },
  actions: {
    getUserAccounts( context ) {
      Interop.getUserAccounts( )
        .then( res => {
          context.commit( 'SET_ACCOUNTS', JSON.parse( res ) )
        } )
        .catch( err => {} )
    },
    getFileStreams( context ) {
      Interop.getFileStreams( )
        .then( res => {
          context.commit( 'SET_CLIENTS', JSON.parse( res ) )
        } )
        .catch( err => { } )
    },
    removeClient( context, payload ) {
      console.log( 'removing: '  + payload.clientId )
      Interop.removeClient( payload.clientId )
      .then( res => {
        console.log( res )
        context.commit( 'REMOVE_CLIENT', payload.clientId )
      })
      .catch( err => { })
    }
  },
  mutations: {
    SET_ACCOUNTS( state, payload ) {
      state.accounts = payload
    },
    ADD_ACCOUNTS( state, payload ) {
      state.accounts = [ ...state.accounts, ...payload ]
    },
    DELETE_ACCOUNT( state, payload ) {
      state.accounts = state.accounts.filter( item => item.fileName !== payload )
    },
    ADD_CLIENT( state, payload ) {  
      payload.client.stream = payload.stream
      
      // extra props
      payload.client.log = [ { timestamp: new Date(), message: 'Client added.' } ]
      payload.client.isLoading = false
      payload.client.error = null
      payload.client.expired = false
      payload.client.lastUpdate = new Date()

      state.clients.unshift( payload.client )
    },
    PURGE_CLIENTS( state ) {
      state.clients = []
    },
    REMOVE_CLIENT( state, payload ) {
      state.clients = state.clients.filter( client => client.ClientId !== payload )
    },
    SET_CLIENTS( state, payload ) {

    },
    APPEND_LOG( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if( !client ) return console.warn( 'No client found!' )

      client.log.unshift( { timestamp: new Date(), message: payload.data } )
      if ( client.log.length > 42 ) log.pop( )
    },
    SET_LOADING( state, payload ) { 
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if( !client ) return console.warn( 'No client found!' )

      client.isLoading = payload.status
    },
    SET_EXPIRED( state, payload ) { 
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if( !client ) return console.warn( 'No client found!' )

      client.expired = payload.status
    },
    SET_METADATA( state, payload ) { 
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if( !client ) return console.warn( 'No client found!' )

      client.stream.name = payload.stream.name
      client.stream.layers = payload.stream.layers

      client.lastUpdate = new Date()
      client.expired = false
    },
  }
} )
