import Vue from 'vue'
import Vuex from 'vuex'

Vue.use( Vuex )

export default new Vuex.Store( {
  state: {
    accounts: [ ],
    clients: [ ],
    selection: [ ]
  },
  getters: {
    accounts: state => state.accounts,
    clients: state => state.clients,
    selection: state => state.selection
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
    SET_CLIENT_CHILDREN( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if( !client ) return console.warn( 'No client found!' )
      console.log( payload )
      client.stream.children = payload.data.children
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
      if( !client.log ) client.log = []
      client.log.unshift( { timestamp: new Date(), message: payload.data } )
      if ( client.log.length > 5 ) { 
        client.log.pop( )
      }
    },
    SET_ERROR( state, payload ) {
      let client = state.clients.find( c => c.stream.streamId === payload.streamId )
      if( !client ) return console.warn( 'No client found!' )
      client.error = payload.data
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
    SET_SELECTION( state, payload ) {
      console.log( payload )
      state.selection = []
      for(const key in payload.selectionInfo )
        state.selection.push( { layer: key, objectCount: payload.selectionInfo[ key ] } )
    }
  }
} )
