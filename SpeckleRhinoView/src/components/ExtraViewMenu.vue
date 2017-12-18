<template>
  <v-menu bottom left>
    <v-btn icon slot="activator" small class='ma-0'>
      <v-icon class='xs-actions'>more_vert</v-icon>
    </v-btn>
    <v-list c>
      <v-list-tile @click='viewStream'>
        <v-list-tile-title class=''>
          View stream
        </v-list-tile-title>
      </v-list-tile>
      <v-list-tile @click='viewData'>
        <v-list-tile-title class=''>
          View data
        </v-list-tile-title>
      </v-list-tile>
      <v-list-tile @click='viewObjects'>
        <v-list-tile-title class=''>
          View objects
        </v-list-tile-title>
      </v-list-tile>
    </v-list>
  </v-menu>
</template>
<script>
export default {
  name: '',
  props: {
    streamId: String,
    restApi: String
  },
  components: {},
  computed: {},
  data( ) {
    return {}
  },
  methods: {
    viewStream( ) {
      Interop.openUrl( this.restApi.replace('api', 'view') + '/?' + this.streamId )
    },
    viewData( ) {
      Interop.openUrl( this.restApi + '/streams/' + this.streamId )
    },
    viewObjects( ) {
      Interop.openUrl( this.restApi + '/streams/' + this.streamId + '/objects/?omit=displayValue,base64' )
    }
  },
  mounted( ) {}
}
</script>
<style lang='scss'>
</style>